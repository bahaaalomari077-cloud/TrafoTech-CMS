using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.Email;
using OrchardCore.Media;

namespace TrafoTech.Admin.Newsletter;

/// <summary>
/// Resolves a campaign's audience and sends it one message per person.
///
/// Sending individually rather than by BCC is deliberate: a single message with
/// many recipients exposes the whole list to everyone on it, and providers treat
/// large BCC blocks as bulk mail. One failure also must not stop the run, so
/// each address is attempted independently and the outcome recorded per person.
/// </summary>
public sealed class CampaignSender
{
    private readonly IContentManager _contentManager;
    private readonly IEmailService _emailService;
    private readonly IMediaFileStore _mediaFileStore;
    private readonly NewsletterRenderer _renderer;
    private readonly ILogger<CampaignSender> _logger;

    public CampaignSender(
        IContentManager contentManager,
        IEmailService emailService,
        IMediaFileStore mediaFileStore,
        NewsletterRenderer renderer,
        ILogger<CampaignSender> logger)
    {
        _contentManager = contentManager;
        _emailService = emailService;
        _mediaFileStore = mediaFileStore;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task<CampaignSendResult> SendAsync(string campaignContentItemId)
    {
        var campaign = await _contentManager.GetAsync(campaignContentItemId, VersionOptions.Latest);
        if (campaign is null)
        {
            return CampaignSendResult.Failed("That campaign no longer exists.");
        }

        JsonNode content = campaign.Content;
        if (content is not JsonObject root || root["Campaign"] is not JsonObject part)
        {
            return CampaignSendResult.Failed("That item is not a campaign.");
        }

        var subject = Text(part, "Subject");
        if (string.IsNullOrWhiteSpace(subject))
        {
            return CampaignSendResult.Failed("The campaign has no subject.");
        }

        var body = Html(part, "Body");
        var attachmentPath = FirstMediaPath(part, "Attachment");

        var recipients = await ResolveRecipientsAsync(part);
        if (recipients.Count == 0)
        {
            return CampaignSendResult.Failed(
                "No subscribed recipients were selected. Pick people or groups first.");
        }

        // Files are read once; every message then gets its own stream over the
        // same bytes, because a stream is consumed by the first send using it.
        var attachments = new List<(string Filename, byte[] Bytes)>();
        var inlineImages = new Dictionary<string, string>(StringComparer.Ordinal);

        var sections = NewsletterModelReader.ReadSections(root);
        foreach (var path in sections
                     .SelectMany(s => s.Items)
                     .Select(i => i.ImagePath)
                     .Where(p => !string.IsNullOrWhiteSpace(p))
                     .Distinct(StringComparer.Ordinal))
        {
            var read = await ReadAttachmentAsync(path);
            if (read is not { } file)
            {
                continue;
            }

            attachments.Add(file);
            inlineImages[path!] = file.Filename;
        }

        var attachment = await ReadAttachmentAsync(attachmentPath);
        if (attachment is { } lead)
        {
            attachments.Add(lead);
        }

        var branding = await LoadBrandingAsync(attachments);

        // A campaign with sections renders as a newsletter; without them it
        // stays the simple single-message form, so both remain usable.
        var html = sections.Count > 0
            ? _renderer.Render(
                Text(part, "Edition"),
                body,
                sections,
                Text(part, "FooterNote"),
                inlineImages,
                rightToLeft: ContainsArabic(subject) || ContainsArabic(body),
                branding: branding)
            : BuildBody(body, attachment?.Filename);

        var sent = 0;
        var failures = new List<string>();

        foreach (var (name, address) in recipients)
        {
            var message = new MailMessage
            {
                To = address,
                // Placeholders are filled per person, so one campaign greets
                // each reader by their own name.
                Subject = Personalise(subject, name, address, asHtml: false),
                Body = Personalise(html, name, address, asHtml: true),
                IsHtmlBody = true,
            };

            foreach (var file in attachments)
            {
                message.Attachments.Add(new MailMessageAttachment
                {
                    Filename = file.Filename,
                    Stream = new MemoryStream(file.Bytes),
                });
            }

            // A body that is empty or whitespace-only gives some providers
            // nothing to deliver, so the send reports success and vanishes.
            if (string.IsNullOrWhiteSpace(message.Body))
            {
                message.Body = subject;
            }

            try
            {
                // The provider name is left null so Orchard uses the configured
                // default, which is whatever the admin set under Email settings.
                var result = await _emailService.SendAsync(message, null!);

                // Log the provider's own response: a success flag alone hides
                // whether the message actually reached the server.
                _logger.LogWarning(
                    "SMTP response for {Address}: succeeded={Succeeded} response={Response}",
                    address, result.Succeeded, result.Response ?? "(null)");

                if (result.Succeeded)
                {
                    sent++;
                }
                else
                {
                    failures.Add($"{address}: {string.Join("; ", result.Errors.SelectMany(e => e.Value))}");
                }
            }
            catch (Exception ex)
            {
                // One bad address must not abort the whole run.
                _logger.LogError(ex, "Campaign send failed for {Address}.", address);
                failures.Add($"{address}: {ex.Message}");
            }
        }

        return new CampaignSendResult(sent, failures);
    }

    /// <summary>
    /// Collects the individually picked people plus everyone in the picked
    /// groups, dropping unsubscribed and duplicate addresses.
    /// </summary>
    private async Task<List<(string Name, string Address)>> ResolveRecipientsAsync(JsonObject part)
    {
        var ids = new List<string>(PickedIds(part, "Recipients"));

        foreach (var groupId in PickedIds(part, "RecipientGroups"))
        {
            var group = await _contentManager.GetAsync(groupId, VersionOptions.Latest);
            if (group is null)
            {
                continue;
            }

            JsonNode groupContent = group.Content;
            if (groupContent is JsonObject groupRoot &&
                groupRoot["ContactGroup"] is JsonObject groupPart)
            {
                ids.AddRange(PickedIds(groupPart, "Members"));
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recipients = new List<(string, string)>();

        foreach (var id in ids.Distinct(StringComparer.Ordinal))
        {
            var contact = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (contact is null)
            {
                continue;
            }

            JsonNode contactContent = contact.Content;
            if (contactContent is not JsonObject contactRoot ||
                contactRoot["Contact"] is not JsonObject contactPart)
            {
                continue;
            }

            // An unsubscribed contact stays in the list but is never mailed.
            if (contactPart["Subscribed"]?["Value"]?.GetValue<bool>() == false)
            {
                continue;
            }

            var address = Text(contactPart, "Email");
            if (string.IsNullOrWhiteSpace(address) || !seen.Add(address))
            {
                continue;
            }

            recipients.Add((contact.DisplayText ?? address, address));
        }

        return recipients;
    }

    /// <summary>
    /// Builds the saved header and footer for every issue.
    ///
    /// Settings live in a single "Newsletter settings" item. Whatever it leaves
    /// blank is taken from the site footer, which already holds the real brand
    /// name, email and phone — so the newsletter matches the site without the
    /// same details being typed twice.
    /// </summary>
    private async Task<NewsletterBranding> LoadBrandingAsync(
        List<(string Filename, byte[] Bytes)> attachments)
    {
        var settings = await _contentManager.GetAsync(NewsletterSettingsId, VersionOptions.Latest);
        JsonObject? part = null;
        JsonObject? root = null;

        if (settings is not null)
        {
            JsonNode content = settings.Content;
            root = content as JsonObject;
            part = root?["NewsletterSettings"] as JsonObject;
        }

        // The site footer is the fallback source for anything not overridden.
        var footer = await _contentManager.GetAsync(SiteFooterId, VersionOptions.Latest);
        JsonObject? footerPart = null;
        if (footer is not null)
        {
            JsonNode content = footer.Content;
            footerPart = (content as JsonObject)?["SiteFooter"] as JsonObject;
        }

        string Pick(string field) =>
            First(part is null ? null : Text(part, field),
                  footerPart is null ? null : Text(footerPart, field));

        // The logo rides along as an inline attachment, like the news images.
        string? logoCid = null;
        var logoPath = part is null ? null : FirstMediaPath(part, "Logo");
        if (await ReadAttachmentAsync(logoPath) is { } logo)
        {
            attachments.Add(logo);
            logoCid = logo.Filename;
        }

        return new NewsletterBranding
        {
            BrandName = Pick("BrandName"),
            LogoCid = logoCid,
            AccentColour = part is null ? null : Text(part, "AccentColour"),
            Email = Pick("Email"),
            Phone = Pick("Phone"),
            Address = part is null ? null : Text(part, "Address"),
            WebsiteUrl = part is null ? null : Text(part, "WebsiteUrl"),
            FooterText = part is null ? null : Text(part, "FooterText"),
            UnsubscribeUrl = part is null ? null : Text(part, "UnsubscribeUrl"),
            Copyright = footerPart is null ? null : Text(footerPart, "Copyright"),
            ComplianceNote = footerPart is null ? null : Text(footerPart, "ComplianceNote"),
            // Links added under Newsletter settings win, so the newsletter can
            // point somewhere different from the site; the site footer is only
            // the fallback when none were added there.
            SocialLinks = ReadSocialLinks(root) is { Count: > 0 } own
                ? own
                : ReadSocialLinks(footerPart),
        };
    }

    private static IReadOnlyList<NewsletterSocialLink> ReadSocialLinks(JsonObject? owner)
    {
        if (owner?["SocialLinks"]?["ContentItems"] is not JsonArray items)
        {
            return [];
        }

        var links = new List<NewsletterSocialLink>();
        foreach (var item in items.OfType<JsonObject>())
        {
            var social = item["SocialLink"] as JsonObject;
            // SocialLink stores a plain text URL and a Network choice — there is
            // no LinkField here, so reading Link.Url found nothing.
            var url = social?["Url"]?["Text"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            // The network name doubles as the label: an <img> per icon would add
            // an attachment for every link.
            var label = item["DisplayText"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(label))
            {
                label = NetworkLabel(social?["Network"]?["Text"]?.GetValue<string>()) ?? url;
            }

            links.Add(new NewsletterSocialLink(label, url));
        }

        return links;
    }

    /// <summary>Turns the stored network key into the name a reader sees.</summary>
    private static string? NetworkLabel(string? network) => network?.ToLowerInvariant() switch
    {
        "facebook" => "Facebook",
        "instagram" => "Instagram",
        "linkedin" => "LinkedIn",
        "x" => "X",
        "youtube" => "YouTube",
        "whatsapp" => "WhatsApp",
        _ => null,
    };

    private static string First(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback ?? string.Empty;

    private const string NewsletterSettingsId = "travotechnewsletter0000000";
    private const string SiteFooterId = "trafotechfooter00000000000";

    /// <summary>
    /// Reads the uploaded file so it can travel with the message.
    ///
    /// A greeting card has to be visible in the email itself, so the file is
    /// attached rather than linked: a link needs the reader to click, and it
    /// breaks entirely once the site is unreachable from their network.
    /// Returns null when there is no attachment or the file cannot be read.
    /// </summary>
    private async Task<(string Filename, byte[] Bytes)?> ReadAttachmentAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            await using var stream = await _mediaFileStore.GetFileStreamAsync(path);
            if (stream is null)
            {
                return null;
            }

            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            return (Path.GetFileName(path), buffer.ToArray());
        }
        catch (Exception ex)
        {
            // A missing or unreadable file must not stop the campaign going out.
            _logger.LogError(ex, "Could not read campaign attachment {Path}.", path);
            return null;
        }
    }

    /// <summary>
    /// Shows an attached image inside the message body via its content id, so
    /// the picture appears in the email rather than only as a file to open.
    /// Non-image attachments are left to the client's attachment list.
    /// </summary>
    private static string BuildBody(string body, string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename) || !IsImage(filename))
        {
            return body;
        }

        return body +
            $"<p style=\"margin-top:16px\">" +
            $"<img src=\"cid:{filename}\" alt=\"\" style=\"max-width:100%;height:auto;border-radius:8px\" />" +
            $"</p>";
    }

    /// <summary>Any Arabic letter flips the newsletter to right-to-left.</summary>
    private static bool ContainsArabic(string? text) =>
        !string.IsNullOrEmpty(text) && text.Any(c => c >= '؀' && c <= 'ۿ');

    /// <summary>
    /// Substitutes the per-recipient placeholders an author can type into the
    /// subject or the message.
    ///
    /// The name is HTML-encoded because it lands inside markup: a contact
    /// called "Ben &amp; Co" would otherwise break the surrounding tags. First
    /// name is taken as the leading word, which suits "Dear {{FirstName}}".
    /// </summary>
    private static string Personalise(string? text, string name, string address, bool asHtml)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var full = (name ?? string.Empty).Trim();
        var first = full.Length == 0
            ? string.Empty
            : full.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        // Encoding belongs to the body only: a subject line is plain text, and
        // encoding it would show "&amp;" to the reader instead of "&".
        string Value(string raw) => asHtml ? WebUtility.HtmlEncode(raw) : raw;

        return text
            .Replace("{{Name}}", Value(full), StringComparison.OrdinalIgnoreCase)
            .Replace("{{FirstName}}", Value(first), StringComparison.OrdinalIgnoreCase)
            .Replace("{{Email}}", Value(address), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImage(string filename) =>
        Path.GetExtension(filename).ToLowerInvariant()
            is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".bmp";

    private static IEnumerable<string> PickedIds(JsonObject part, string field) =>
        part[field]?["ContentItemIds"] is JsonArray array
            ? array.Select(n => n?.GetValue<string>()).Where(s => !string.IsNullOrEmpty(s))!
            : [];

    private static string Text(JsonObject part, string field) =>
        part[field]?["Text"]?.GetValue<string>() ?? string.Empty;

    private static string Html(JsonObject part, string field) =>
        part[field]?["Html"]?.GetValue<string>() ?? string.Empty;

    private static string? FirstMediaPath(JsonObject part, string field) =>
        part[field]?["Paths"] is JsonArray paths && paths.Count > 0
            ? paths[0]?.GetValue<string>()
            : null;
}

/// <param name="Sent">How many messages the provider accepted.</param>
/// <param name="Failures">One entry per address that could not be sent.</param>
public sealed record CampaignSendResult(int Sent, IReadOnlyList<string> Failures)
{
    public string? Error { get; private init; }

    public bool IsFailure => Error is not null;

    public static CampaignSendResult Failed(string error) =>
        new(0, []) { Error = error };
}
