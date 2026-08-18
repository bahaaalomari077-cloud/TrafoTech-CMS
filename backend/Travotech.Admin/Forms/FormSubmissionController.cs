using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.Email;

namespace TrafoTech.Admin.Forms;

/// <summary>
/// Receives a form submission from the site.
///
/// This is deliberately the only anonymous write endpoint in the application.
/// It cannot create arbitrary content: the payload is reduced to a fixed
/// FormSubmission item with three text fields, so a caller cannot choose a
/// content type or set parts of its own.
/// </summary>
[ApiController]
[Route("api/forms")]
// Orchard requires an antiforgery token on POST by default. This endpoint is
// called by the Angular app from another origin, which has no such token, so
// the check is turned off here — the payload is validated and reduced to a
// fixed shape instead, and CORS limits who may call it.
[IgnoreAntiforgeryToken]
public sealed class FormSubmissionController : ControllerBase
{
    /// <summary>Caps what one caller can store, so the endpoint cannot be used to fill the database.</summary>
    private const int MaxFields = 40;
    private const int MaxValueLength = 4000;

    private readonly IContentManager _contentManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<FormSubmissionController> _logger;

    public FormSubmissionController(
        IContentManager contentManager,
        IEmailService emailService,
        ILogger<FormSubmissionController> logger)
    {
        _contentManager = contentManager;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] FormSubmissionRequest request)
    {
        if (request?.Answers is null || request.Answers.Count == 0)
        {
            return BadRequest(new { ok = false, message = "The submission was empty." });
        }

        // A hidden field no human fills in: anything with a value here is a bot,
        // and is accepted silently so the sender learns nothing.
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            _logger.LogInformation("Discarded a honeypot submission for {Form}.", request.FormName);
            return Ok(new { ok = true });
        }

        var answers = request.Answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Label))
            .Take(MaxFields)
            .Select(a => (
                Label: Trim(a.Label!, 200),
                Value: Trim(a.Value ?? string.Empty, MaxValueLength)))
            .ToList();

        if (answers.Count == 0)
        {
            return BadRequest(new { ok = false, message = "The submission was empty." });
        }

        var formName = Trim(request.FormName ?? "Form", 200);
        var received = DateTime.UtcNow;

        var item = await _contentManager.NewAsync("FormSubmission");
        var reference = $"{formName} — {received:yyyy-MM-dd HH:mm} UTC";
        item.DisplayText = reference;
        item.Alter<OrchardCore.Title.Models.TitlePart>(p => p.Title = reference);

        var body = new StringBuilder();
        foreach (var (label, value) in answers)
        {
            body.Append(label).Append(": ").AppendLine(value);
        }

        JsonNode content = item.Content;
        if (content is JsonObject root)
        {
            root["FormSubmission"] = new JsonObject
            {
                ["FormName"] = new JsonObject { ["Text"] = formName },
                ["Answers"] = new JsonObject { ["Text"] = body.ToString() },
                ["SubmittedAt"] = new JsonObject { ["Text"] = received.ToString("u") },
            };
        }

        await _contentManager.CreateAsync(item, VersionOptions.Draft);
        await _contentManager.PublishAsync(item);

        // The recipient is read from the block, never from the request: taking
        // it from the caller would turn this open endpoint into a relay for
        // sending mail to any address.
        var notifyTo = await ResolveNotifyAddressAsync(request.FormBlockId);

        // The notification is secondary: the answer is already stored, so a mail
        // failure must not lose it or fail the caller's request.
        if (!string.IsNullOrWhiteSpace(notifyTo))
        {
            try
            {
                var message = new MailMessage
                {
                    To = notifyTo,
                    Subject = $"New submission: {formName}",
                    Body = body.ToString().Replace("\n", "<br />"),
                    IsHtmlBody = true,
                };

                var result = await _emailService.SendAsync(message, null!);
                if (!result.Succeeded)
                {
                    _logger.LogWarning(
                        "Stored submission for {Form} but the notification failed: {Errors}",
                        formName, string.Join("; ", result.Errors.SelectMany(e => e.Value)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stored submission for {Form} but could not notify.", formName);
            }
        }

        return Ok(new { ok = true });
    }

    /// <summary>
    /// Reads the notify address from the form block itself.
    ///
    /// Returns null when the block is missing or has no address, so an unknown
    /// id simply means "store it, notify no one" rather than an error.
    /// </summary>
    private async Task<string?> ResolveNotifyAddressAsync(string? formBlockId)
    {
        if (string.IsNullOrWhiteSpace(formBlockId))
        {
            return null;
        }

        // A block inside a bag has no record of its own, so it cannot be fetched
        // by id. It is found by scanning the published pages for the matching
        // block — the set is small, and this keeps the address server-side.
        foreach (var pageId in await PublishedPageIdsAsync())
        {
            var page = await _contentManager.GetAsync(pageId, VersionOptions.Published);
            if (page is null)
            {
                continue;
            }

            JsonNode content = page.Content;
            if (content is not JsonObject root)
            {
                continue;
            }

            foreach (var section in Array(root["Sections"]))
            {
                foreach (var block in Array(section["Blocks"]))
                {
                    if (block["ContentItemId"]?.GetValue<string>() != formBlockId)
                    {
                        continue;
                    }

                    return block["FormBlock"]?["NotifyEmail"]?["Text"]?.GetValue<string>();
                }
            }
        }

        return null;
    }

    /// <summary>The seeded pages a form block can live on.</summary>
    private static Task<string[]> PublishedPageIdsAsync() =>
        Task.FromResult(new[]
        {
            "trafotechhome0000000000000", "trafotechhomeen00000000000",
            "trafotechabout000000000000", "trafotechabouten0000000000",
            "trafotechcontact0000000000", "trafotechcontacten00000000",
            "trafotechproducts000000000", "trafotechproductsen0000000",
        });

    private static IEnumerable<JsonObject> Array(JsonNode? bag) =>
        bag?["ContentItems"] is JsonArray items
            ? items.OfType<JsonObject>()
            : [];

    private static string Trim(string value, int max) =>
        value.Length <= max ? value : value[..max];
}

public sealed class FormSubmissionRequest
{
    public string? FormName { get; set; }

    /// <summary>
    /// The page carrying the form. The notify address is looked up from it, so
    /// the caller cannot choose who gets emailed.
    /// </summary>
    public string? FormBlockId { get; set; }

    /// <summary>Honeypot. Real visitors never see this field, so it stays empty.</summary>
    public string? Website { get; set; }

    public List<FormAnswer>? Answers { get; set; }
}

public sealed class FormAnswer
{
    public string? Label { get; set; }
    public string? Value { get; set; }
}
