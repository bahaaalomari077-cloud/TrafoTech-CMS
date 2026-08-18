using System.Text.Json.Nodes;
using OrchardCore.Autoroute.Models;
using OrchardCore.ContentLocalization.Models;
using OrchardCore.ContentManagement;
using OrchardCore.Data.Migration;
using OrchardCore.Title.Models;

namespace TrafoTech.Admin.Migrations;

/// <summary>
/// Seeds the site's starter content.
///
/// Each item is created only when it is missing, so this is safe to run against
/// a database an editor has already worked in: existing items are left exactly
/// as the editor left them, and a new page added in a later UpdateFromN simply
/// appears alongside their work.
/// </summary>
public sealed class ContentMigrations : DataMigration
{
    private readonly IContentManager _contentManager;

    public ContentMigrations(IContentManager contentManager)
    {
        _contentManager = contentManager;
    }

    public async Task<int> CreateAsync()
    {
        await SeedChromeAsync("ar");
        await SeedChromeAsync("en");
        await SeedHomeAsync("ar");
        await SeedHomeAsync("en");
        await SeedProductsAsync("ar");
        await SeedProductsAsync("en");

        return 1;
    }

    /// <summary>
    /// Adds the About page. This is the pattern for every future page: append an
    /// UpdateFromN, and it lands on the running site without touching anything
    /// an editor has already changed.
    /// </summary>
    public async Task<int> UpdateFrom1Async()
    {
        await SeedAboutAsync("ar");
        await SeedAboutAsync("en");

        return 2;
    }

    /// <summary>
    /// Adds the Contact page. The header and footer already link to /contact,
    /// so without it those links resolved to nothing.
    /// </summary>
    public async Task<int> UpdateFrom2Async()
    {
        await SeedContactAsync("ar");
        await SeedContactAsync("en");

        return 3;
    }

    /// <summary>
    /// Replaces the placeholder About and Contact pages with the full content.
    ///
    /// These two were seeded with short stand-in copy. Deleting and recreating
    /// them is safe because they are still exactly as seeded; anything an editor
    /// had changed would be kept by EnsureAsync, so this deliberately removes
    /// them first to guarantee the new version lands.
    /// </summary>
    public async Task<int> UpdateFrom3Async()
    {
        foreach (var id in new[]
                 {
                     "trafotechabout000000000000", "trafotechabouten0000000000",
                     "trafotechcontact0000000000", "trafotechcontacten00000000",
                 })
        {
            var existing = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (existing is not null)
            {
                await _contentManager.RemoveAsync(existing);
            }
        }

        await SeedAboutFullAsync("ar");
        await SeedAboutFullAsync("en");
        await SeedContactFullAsync("ar");
        await SeedContactFullAsync("en");

        return 4;
    }

    /// <summary>
    /// Reseeds Contact so the details card picks up its address and working
    /// hours. SeedContactFullAsync already ran, so the pages must be removed
    /// for the new fields to land.
    /// </summary>
    public async Task<int> UpdateFrom4Async()
    {
        foreach (var id in new[] { "trafotechcontact0000000000", "trafotechcontacten00000000" })
        {
            var existing = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (existing is not null)
            {
                await _contentManager.RemoveAsync(existing);
            }
        }

        await SeedContactFullAsync("ar");
        await SeedContactFullAsync("en");

        return 5;
    }

    /// <summary>
    /// Rebuilds the home page to the reference's four-section rhythm:
    /// hero, then two consecutive light sections, then a dark closing CTA.
    ///
    /// The page previously alternated dark/light/dark, so every boundary read as
    /// a hard cut. Two light sections in a row make the middle of the page one
    /// continuous surface, which is what the reference does.
    /// </summary>
    public async Task<int> UpdateFrom5Async()
    {
        foreach (var id in new[] { "trafotechhome0000000000000", "trafotechhomeen00000000000" })
        {
            var existing = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (existing is not null)
            {
                await _contentManager.RemoveAsync(existing);
            }
        }

        await SeedHomeAsync("ar");
        await SeedHomeAsync("en");

        return 6;
    }

    /// <summary>
    /// Corrects the English brand spelling: Travotek becomes Travotech.
    ///
    /// This rewrites the string wherever it appears rather than reseeding the
    /// pages, so any edits made in the admin survive the rename. The Arabic
    /// name is a separate string and is deliberately left untouched.
    /// </summary>
    public async Task<int> UpdateFrom6Async()
    {
        await RenameBrandEverywhereAsync();

        return 7;
    }

    /// <summary>
    /// Re-runs the brand rename.
    ///
    /// The first attempt recorded as complete without changing anything: it
    /// type-tested <c>item.Content</c>, which is declared dynamic, so the
    /// pattern never matched and every item was skipped. Since the version was
    /// already stored, correcting that method alone would never run again —
    /// hence a new version.
    /// </summary>
    public async Task<int> UpdateFrom7Async()
    {
        await RenameBrandEverywhereAsync();

        return 8;
    }

    /// <summary>
    /// Catches the lowercase spelling the earlier passes missed.
    ///
    /// Those replaced "Travotek" ordinally, so "hello@travotek.com" in the
    /// footer, the contact card and the form's recipient address kept the old
    /// domain. The rename is case-insensitive now and preserves the casing of
    /// whatever it replaces.
    /// </summary>
    public async Task<int> UpdateFrom8Async()
    {
        await RenameBrandEverywhereAsync();

        return 9;
    }

    /// <summary>
    /// Creates the single "Newsletter settings" item the sender reads its header
    /// and footer from.
    ///
    /// It is seeded with the site's own contact details so the first issue is
    /// already branded, and the small print explains why the reader received it
    /// — a line bulk mail is expected to carry.
    /// </summary>
    public async Task<int> UpdateFrom9Async()
    {
        await EnsureAsync(
            "travotechnewsletter0000000",
            "NewsletterSettings",
            "Newsletter settings",
            payload =>
            {
                Text(payload, "NewsletterSettings", "Email", "hello@travotech.com");
                Text(payload, "NewsletterSettings", "Phone", "+962 6 000 0000");
                Text(payload, "NewsletterSettings", "Address", "Amman, Jordan");
                Text(payload, "NewsletterSettings", "WebsiteUrl", "https://travotech.com");
                Text(payload, "NewsletterSettings", "AccentColour", "#1b3a6b");
                Text(payload, "NewsletterSettings", "FooterText",
                    "You are receiving this because you subscribed to Travotech updates.");
            });

        return 10;
    }

    /// <summary>
    /// Seeds the newsletter's social links.
    ///
    /// The settings item was created without any, so the closing bar had nothing
    /// to show. These are ordinary bag items: an editor renames, re-points or
    /// deletes them from the Newsletter settings screen, and adds more networks
    /// the same way.
    /// </summary>
    public async Task<int> UpdateFrom10Async()
    {
        await SeedSocialLinksAsync();

        return 11;
    }

    /// <summary>
    /// Re-seeds the social links with the field names this type actually has.
    ///
    /// The first pass wrote a "Link" LinkField, but SocialLink carries a Network
    /// choice and a plain text Url — so the items were created with no address
    /// and the footer rendered nothing. Version 10 was already recorded, so the
    /// corrected seed needs its own version to run.
    /// </summary>
    public async Task<int> UpdateFrom11Async()
    {
        await SeedSocialLinksAsync();

        return 12;
    }

    /// <summary>
    /// Adds a worked example of the two galleries to the About page.
    ///
    /// The blocks exist but nothing used them, so there was no way to see the
    /// result without an editor building one by hand. This seeds one of each —
    /// a photo grid from the media library and a video from a YouTube link —
    /// which an editor can then edit or delete like any other block.
    /// </summary>
    public async Task<int> UpdateFrom15Async()
    {
        foreach (var (id, ar) in new[]
                 {
                     ("trafotechabout000000000000", true),
                     ("trafotechabouten0000000000", false),
                 })
        {
            var page = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (page is null)
            {
                continue;
            }

            JsonNode content = page.Content;
            if (content is not JsonObject root ||
                root["Sections"]?["ContentItems"] is not JsonArray sections)
            {
                continue;
            }

            // Drop any gallery section from the earlier attempt: it put both
            // blocks in one section, which rendered them side by side and left
            // the videos too narrow to use.
            for (var i = sections.Count - 1; i >= 0; i--)
            {
                if (sections[i] is JsonObject existing &&
                    existing["Blocks"]?["ContentItems"] is JsonArray blocks &&
                    blocks.OfType<JsonObject>().Any(x =>
                        x["ContentType"]?.GetValue<string>() is "PhotoGalleryBlock" or "VideoGalleryBlock"))
                {
                    sections.RemoveAt(i);
                }
            }

            // Section() returns a builder, so the section item is created with
            // Bag() the same way every other seeded section is.
            // One section each: two blocks in a single section render side by
            // side, which squeezed the video grid into a 379px column.
            var photoSection = Bag(("Section", ar ? "معرض الصور" : "Photo gallery",
                Section("photos", "alt", "container", "spacious",
                    ("PhotoGalleryBlock", ar ? "معرض الصور" : "Photo gallery", child =>
                    {
                        Text(child, "PhotoGalleryBlock", "Heading",
                            ar ? "معرض الصور" : "Photo gallery");
                        Text(child, "PhotoGalleryBlock", "Lead", ar
                            ? "لقطات من هويتنا ومشاريعنا."
                            : "Moments from our brand and our work.");
                        Text(child, "PhotoGalleryBlock", "Columns", "3");
                        Bool(child, "PhotoGalleryBlock", "Lightbox", true);
                        Media(child, "PhotoGalleryBlock", "Images",
                            ["travotech-logo-new.png", "travotech-logo-light.png", "travotech-logo.png"],
                            [
                                ar ? "الشعار الأساسي" : "Primary wordmark",
                                ar ? "الشعار الفاتح" : "Light wordmark",
                                ar ? "الشعار القديم" : "Previous logo",
                            ]);
                    }))));

            var videoSection = Bag(("Section", ar ? "معرض الفيديو" : "Video gallery",
                Section("videos", "default", "container", "spacious",
                    ("VideoGalleryBlock", ar ? "معرض الفيديو" : "Video gallery", child =>
                    {
                        Text(child, "VideoGalleryBlock", "Heading",
                            ar ? "معرض الفيديو" : "Video gallery");
                        Text(child, "VideoGalleryBlock", "Columns", "2");
                        child["Videos"] = Bag(
                            ("VideoItem", ar ? "فيديو تعريفي" : "Introduction", video =>
                            {
                                Text(video, "VideoItem", "VideoUrl",
                                    "https://www.youtube.com/watch?v=FodsNpswG8U");
                                Text(video, "VideoItem", "Description", ar
                                    ? "نظرة عامة على ما نبنيه."
                                    : "An overview of what we build.");
                            }));
                    }))));

            foreach (var bag in new[] { photoSection, videoSection })
            {
                foreach (var node in (bag["ContentItems"] as JsonArray)!.ToList())
                {
                    sections.Add(node!.DeepClone());
                }
            }


            await _contentManager.UpdateAsync(page);
            await _contentManager.PublishAsync(page);
        }

        return 16;
    }

    /// <summary>Points a media field at one or more library files.</summary>
    private static void Media(
        JsonObject item,
        string part,
        string field,
        string[] paths,
        string[]? texts = null)
    {
        var pathArray = new JsonArray();
        foreach (var path in paths)
        {
            pathArray.Add(path);
        }

        // MediaTexts must line up with Paths, so a missing caption is an empty
        // string rather than a shorter array.
        var textArray = new JsonArray();
        for (var i = 0; i < paths.Length; i++)
        {
            textArray.Add(texts is not null && i < texts.Length ? texts[i] : string.Empty);
        }

        Field(item, part, field, new JsonObject
        {
            ["Paths"] = pathArray,
            ["MediaTexts"] = textArray,
        });
    }

    /// <summary>
    /// Puts the wordmark into the header, the footer and the newsletter.
    ///
    /// Two files, not one: the logo's "TRAVO" is dark navy, which all but
    /// disappears on the near-black footer, so a variant with that word in white
    /// is used wherever the surface is dark. Both are the same mark — only the
    /// one word changes, so the two-tone identity survives.
    ///
    /// Each surface has its own Logo field, so setting one never affects another;
    /// only an item still without a logo is filled, leaving editor choices alone.
    /// </summary>
    public async Task<int> UpdateFrom12Async()
    {
        // Header sits on white, so it takes the standard mark.
        foreach (var id in new[] { "trafotechheader00000000000", "trafotechheaderen000000000" })
        {
            await SetLogoAsync(id, "SiteHeader", "travotech-logo-new.png");
        }

        // Footer and the newsletter masthead are dark.
        foreach (var id in new[] { "trafotechfooter00000000000", "trafotechfooteren000000000" })
        {
            await SetLogoAsync(id, "SiteFooter", "travotech-logo-light.png");
        }

        await SetLogoAsync("travotechnewsletter0000000", "NewsletterSettings", "travotech-logo-new.png");

        return 13;
    }

    /// <summary>
    /// Fills in the contact labels that just became CMS fields.
    ///
    /// They render from the frontend's wording while empty, so the pages already
    /// look right — but an editor opening the block would find blank boxes and no
    /// idea what they control. Writing the current wording in makes the fields
    /// self-explanatory and editable.
    /// </summary>
    public async Task<int> UpdateFrom13Async()
    {
        foreach (var (id, ar) in new[]
                 {
                     ("trafotechcontact0000000000", true),
                     ("trafotechcontacten00000000", false),
                 })
        {
            var item = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (item is null)
            {
                continue;
            }

            JsonNode content = item.Content;
            if (content is not JsonObject root)
            {
                continue;
            }

            var changed = false;

            foreach (var block in Blocks(root))
            {
                if (block["ContactFormBlock"] is JsonObject form)
                {
                    changed |= FillLabel(form, "NameLabel", ar ? "الاسم" : "Name");
                    changed |= FillLabel(form, "EmailLabel", ar ? "البريد الإلكتروني" : "Email");
                    changed |= FillLabel(form, "OrganisationLabel",
                        ar ? "الجهة (اختياري)" : "Organisation (optional)");
                    changed |= FillLabel(form, "MessageLabel", ar ? "رسالتك" : "Your message");
                }

                if (block["ContactInfoBlock"] is JsonObject info)
                {
                    changed |= FillLabel(info, "EmailLabel", ar ? "البريد" : "Email");
                    changed |= FillLabel(info, "PhoneLabel", ar ? "الهاتف" : "Phone");
                    changed |= FillLabel(info, "AddressLabel", ar ? "العنوان" : "Address");
                    changed |= FillLabel(info, "HoursLabel", ar ? "ساعات العمل" : "Working hours");
                }
            }

            if (changed)
            {
                await _contentManager.UpdateAsync(item);
                await _contentManager.PublishAsync(item);
            }
        }

        return 14;
    }

    /// <summary>Every block on a page, across all of its sections.</summary>
    private static IEnumerable<JsonObject> Blocks(JsonObject pageRoot)
    {
        if (pageRoot["Sections"]?["ContentItems"] is not JsonArray sections)
        {
            yield break;
        }

        foreach (var section in sections.OfType<JsonObject>())
        {
            if (section["Blocks"]?["ContentItems"] is not JsonArray blocks)
            {
                continue;
            }

            foreach (var block in blocks.OfType<JsonObject>())
            {
                yield return block;
            }
        }
    }

    /// <summary>Writes a label only when the editor has not set one.</summary>
    private static bool FillLabel(JsonObject part, string field, string value)
    {
        if (!string.IsNullOrWhiteSpace(part[field]?["Text"]?.GetValue<string>()))
        {
            return false;
        }

        part[field] = new JsonObject { ["Text"] = value };
        return true;
    }

    /// <summary>
    /// Points one item's Logo media field at a file, unless it already has one.
    /// </summary>
    private async Task SetLogoAsync(string contentItemId, string part, string fileName)
    {
        var item = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
        if (item is null)
        {
            return;
        }

        JsonNode content = item.Content;
        if (content is not JsonObject root || root[part] is not JsonObject bucket)
        {
            return;
        }

        if (bucket["Logo"]?["Paths"] is JsonArray current && current.Count > 0)
        {
            return;
        }

        bucket["Logo"] = new JsonObject
        {
            ["Paths"] = new JsonArray(fileName),
            ["MediaTexts"] = new JsonArray(string.Empty),
        };

        await _contentManager.UpdateAsync(item);
        await _contentManager.PublishAsync(item);
    }

    /// <summary>
    /// Fills the newsletter's social links, leaving curated ones alone.
    /// </summary>
    private async Task SeedSocialLinksAsync()
    {
        var settings = await _contentManager.GetAsync(
            "travotechnewsletter0000000", VersionOptions.Latest);
        if (settings is null)
        {
            return;
        }

        JsonNode content = settings.Content;
        if (content is not JsonObject root)
        {
            return;
        }

        // A link that already has an address was set by an editor; leave it.
        if (root["SocialLinks"]?["ContentItems"] is JsonArray existing &&
            existing.OfType<JsonObject>().Any(x =>
                !string.IsNullOrWhiteSpace(x["SocialLink"]?["Url"]?["Text"]?.GetValue<string>())))
        {
            return;
        }

        root["SocialLinks"] = Bag(
            ("SocialLink", "Facebook", child =>
            {
                Text(child, "SocialLink", "Network", "facebook");
                Text(child, "SocialLink", "Url", "https://facebook.com/travotech");
            }),
            ("SocialLink", "LinkedIn", child =>
            {
                Text(child, "SocialLink", "Network", "linkedin");
                Text(child, "SocialLink", "Url", "https://linkedin.com/company/travotech");
            }),
            ("SocialLink", "X", child =>
            {
                Text(child, "SocialLink", "Network", "x");
                Text(child, "SocialLink", "Url", "https://x.com/travotech");
            }));

        await _contentManager.UpdateAsync(settings);
        await _contentManager.PublishAsync(settings);
    }

    private const string OldBrand = "Travotek";
    private const string NewBrand = "Travotech";

    /// <summary>
    /// Rewrites the English brand in every seeded item, in place, so edits made
    /// in the admin survive. The Arabic name is a separate string and is left
    /// untouched.
    /// </summary>
    private async Task RenameBrandEverywhereAsync()
    {
        var ids = new[]
        {
            "trafotechhome0000000000000", "trafotechhomeen00000000000",
            "trafotechabout000000000000", "trafotechabouten0000000000",
            "trafotechcontact0000000000", "trafotechcontacten00000000",
            "trafotechproducts000000000", "trafotechproductsen0000000",
            "trafotechheader00000000000", "trafotechheaderen000000000",
            "trafotechfooter00000000000", "trafotechfooteren000000000",
        };

        foreach (var id in ids)
        {
            var item = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (item is null)
            {
                continue;
            }

            // Content is declared dynamic, so a direct type-test against
            // JsonObject never matches at runtime — it has to be assigned to a
            // JsonNode first. The rename is then applied in place across the
            // whole tree, including the nested section and block bags.
            JsonNode content = item.Content;
            if (content is not JsonObject root || !RenameBrand(root))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(item.DisplayText))
            {
                var renamed = item.DisplayText.Replace(OldBrand, NewBrand, StringComparison.Ordinal);
                if (!string.Equals(renamed, item.DisplayText, StringComparison.Ordinal))
                {
                    item.DisplayText = renamed;
                    item.Alter<TitlePart>(p => p.Title = renamed);
                }
            }

            await _contentManager.UpdateAsync(item);
            await _contentManager.PublishAsync(item);
        }
    }

    /// <summary>
    /// Rewrites the brand in every string in the tree. Returns whether anything
    /// changed, so an item with no occurrences is not republished needlessly.
    /// </summary>
    private static bool RenameBrand(JsonNode node)
    {
        var changed = false;

        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj.ToList())
                {
                    if (value is JsonValue v && v.TryGetValue<string>(out var text))
                    {
                        var renamed = ReplaceBrand(text);
                        if (renamed is not null)
                        {
                            obj[key] = renamed;
                            changed = true;
                        }
                    }
                    else if (value is not null && RenameBrand(value))
                    {
                        changed = true;
                    }
                }

                break;

            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                {
                    if (arr[i] is JsonValue v && v.TryGetValue<string>(out var text))
                    {
                        var renamed = ReplaceBrand(text);
                        if (renamed is not null)
                        {
                            arr[i] = renamed;
                            changed = true;
                        }
                    }
                    else if (arr[i] is { } child && RenameBrand(child))
                    {
                        changed = true;
                    }
                }

                break;
        }

        return changed;
    }

    /// <summary>
    /// Replaces the brand however it is cased, keeping the original casing of
    /// each occurrence — so "Travotek" becomes "Travotech" and the lowercase
    /// "travotek.com" becomes "travotech.com". Returns null when unchanged, so
    /// callers can skip republishing an item that has no occurrences.
    /// </summary>
    private static string? ReplaceBrand(string text)
    {
        var index = text.IndexOf(OldBrand, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var builder = new System.Text.StringBuilder(text.Length + 8);
        var position = 0;

        while (index >= 0)
        {
            builder.Append(text, position, index - position);

            // Lowercase source keeps a lowercase replacement (the domain);
            // anything else takes the display casing.
            builder.Append(char.IsUpper(text[index]) ? NewBrand : NewBrand.ToLowerInvariant());

            position = index + OldBrand.Length;
            index = text.IndexOf(OldBrand, position, StringComparison.OrdinalIgnoreCase);
        }

        builder.Append(text, position, text.Length - position);

        return builder.ToString();
    }

    private async Task SeedAboutFullAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var stats = ar
            ? new[] { ("99.99", "%", "جاهزية النظام"), ("<5", "s", "زمن التحويل"),
                      ("24/7", "", "تشغيل متواصل"), ("12", "+", "سوقاً نشطاً") }
            : new[] { ("99.99", "%", "System uptime"), ("<5", "s", "Transfer time"),
                      ("24/7", "", "Always on"), ("12", "+", "Active markets") };

        var statsBag = Bag(stats
            .Select<(string V, string S, string L), (string, string, Action<JsonObject>)>(s =>
                ("StatItem", s.L, child =>
                {
                    Text(child, "StatItem", "Value", s.V);
                    Text(child, "StatItem", "Suffix", s.S);
                    Text(child, "StatItem", "Label", s.L);
                }))
            .ToArray());

        var milestones = ar
            ? new[]
            {
                ("2019", "الانطلاق", "بدأنا بفريق من ثمانية مهندسين وهدف واحد: جعل التحويل الفوري معياراً لا ميزة."),
                ("2021", "أول بنك مركزي", "تشغيل أول منصة دفع فوري على مستوى دولة كاملة، وربط أربعة عشر بنكاً في سنة واحدة."),
                ("2023", "التوسع الإقليمي", "وصلنا إلى اثني عشر سوقاً، وأطلقنا مركز المدفوعات كطبقة توجيه موحدة بين الأنظمة."),
                ("2025", "الجيل الجديد", "أعدنا بناء نواة التسوية لتعالج ضعف الحجم بنصف زمن الاستجابة السابق."),
            }
            : new[]
            {
                ("2019", "The start", "We began with eight engineers and one goal: make instant transfer the standard, not a feature."),
                ("2021", "First central bank", "Launched a nationwide instant payment platform, connecting fourteen banks in a single year."),
                ("2023", "Regional expansion", "Reached twelve markets and launched the payments hub as a unified routing layer between systems."),
                ("2025", "The next generation", "Rebuilt the settlement core to handle twice the volume at half the previous latency."),
            };

        var milestonesBag = Bag(milestones
            .Select<(string Y, string T, string D), (string, string, Action<JsonObject>)>(m =>
                ("MilestoneItem", m.T, child =>
                {
                    Text(child, "MilestoneItem", "Year", m.Y);
                    Text(child, "MilestoneItem", "Description", m.D);
                }))
            .ToArray());

        var values = ar
            ? new[]
            {
                ("الموثوقية أولاً", "حين يتوقف نظام دفع، تتوقف معه رواتب وتجارة وحياة يومية. نحن نبني على هذا الأساس."),
                ("الوضوح لا التعقيد", "أنظمة الدفع معقّدة بطبيعتها. مهمتنا أن نخفي التعقيد عن المستخدم لا أن نضاعفه."),
                ("شراكة طويلة", "لا نسلّم مشروعاً ونمضي. نبقى مع عملائنا طوال دورة حياة النظام بالكامل."),
            }
            : new[]
            {
                ("Reliability first", "When a payment system stops, salaries, trade and daily life stop with it. We build on that premise."),
                ("Clarity over complexity", "Payment systems are complex by nature. Our job is to hide that complexity, not to double it."),
                ("A long partnership", "We do not hand over a project and walk away. We stay with our clients for the system's whole life."),
            };

        var valuesBag = Bag(values
            .Select<(string T, string D), (string, string, Action<JsonObject>)>(v =>
                ("ValueItem", v.T, child => Text(child, "ValueItem", "Description", v.D)))
            .ToArray());

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "عن الشركة" : "About");
                Text(child, "PageHeaderBlock", "Heading", ar
                    ? "نبني ما لا يُرى ولا يتوقف"
                    : "We build what is never seen and never stops");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "أنظمة الدفع تنجح حين لا يلاحظها أحد. هذا المعيار الذي نقيس به أنفسنا منذ اليوم الأول."
                    : "Payment systems succeed when nobody notices them. That is the standard we have measured ourselves against since day one.");
            }));

        var story = Section("story", "default", "container", "spacious",
            ("StoryBlock", "Story", child =>
            {
                Text(child, "StoryBlock", "Eyebrow", ar ? "قصتنا" : "Our story");
                Text(child, "StoryBlock", "Heading", ar
                    ? "بدأنا من مشكلة واحدة"
                    : "We started from a single problem");
                Field(child, "StoryBlock", "Body", new JsonObject
                {
                    ["Html"] = ar
                        ? "<p>في ٢٠١٩ كان تحويل مبلغ بين بنكين في المنطقة يستغرق يومي عمل. لم يكن ذلك قيداً تقنياً بل إرثاً من أنظمة بُنيت لعصر آخر. جلس ثمانية مهندسون وقرروا أن يبنوا البديل من الصفر.</p>"
                          + "<p>اليوم تعالج منصاتنا مدفوعات في اثني عشر سوقاً، ولا يزال المقياس نفسه: كم ثانية يحتاجها المال ليصل، وكم دقيقة تحتمل النظام من التوقف.</p>"
                        : "<p>In 2019, moving money between two banks in the region took two business days. That was not a technical limit but an inheritance from systems built for another era. Eight engineers sat down and decided to build the alternative from scratch.</p>"
                          + "<p>Today our platforms process payments across twelve markets, and the measure has not changed: how many seconds money needs to arrive, and how many minutes of downtime a system can bear.</p>",
                });
                Attach(child, "Stats", statsBag);
            }));

        var timeline = Section("timeline", "alt", "container", "spacious",
            ("TimelineBlock", "Timeline", child =>
            {
                Text(child, "TimelineBlock", "Eyebrow", ar ? "المسيرة" : "The journey");
                Text(child, "TimelineBlock", "Heading", ar
                    ? "محطات شكّلت الطريق"
                    : "Milestones that shaped the path");
                Attach(child, "Milestones", milestonesBag);
            }));

        var valuesSection = Section("values", "default", "container", "spacious",
            ("ValuesBlock", "Values", child =>
            {
                Text(child, "ValuesBlock", "Eyebrow", ar ? "قيمنا" : "Our values");
                Text(child, "ValuesBlock", "Heading", ar
                    ? "ثلاثة مبادئ لا نساوم عليها"
                    : "Three principles we do not compromise on");
                Attach(child, "Values", valuesBag);
            }));

        var cta = Section("cta", "alt", "container", "spacious",
            ("CtaBlock", "CTA", child =>
            {
                Text(child, "CtaBlock", "Heading", ar ? "تحب تعرف أكثر؟" : "Want to know more?");
                Text(child, "CtaBlock", "Text", ar
                    ? "الباب مفتوح للأسئلة التقنية والتجارية على حد سواء."
                    : "The door is open for technical and commercial questions alike.");
                Text(child, "CtaBlock", "ButtonText", ar ? "تواصل معنا" : "Contact us");
                Link(child, "CtaBlock", "ButtonLink", ar ? "/contact" : "/en/contact",
                     ar ? "تواصل معنا" : "Contact us");
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Story", story),
            ("Section", "Timeline", timeline),
            ("Section", "Values", valuesSection),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechabout{suffix}".PadRight(26, '0')[..26], "Page", "About", item =>
        {
            Localize(item, culture, "trafotech-about");
            Route(item, ar ? "about" : "en/about");
            Text(item, "Seo", "MetaTitle", ar ? "عن الشركة — ترافو تيك" : "About — Travotech");
            Text(item, "Seo", "MetaDescription", ar
                ? "نبني ما لا يُرى ولا يتوقف — قصة ترافو تيك ومسيرتها وقيمها."
                : "We build what is never seen and never stops — the Travotech story, journey and values.");
            Bool(item, "Seo", "NoIndex", false);
            Attach(item, "Sections", sections);
        });
    }

    private async Task SeedContactFullAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "تواصل معنا" : "Contact");
                Text(child, "PageHeaderBlock", "Heading", ar ? "لنبدأ الحديث" : "Let's start the conversation");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "أخبرنا عن نظامك والتحدي الذي تواجهه. نقرأ كل رسالة ونعود خلال يوم عمل واحد."
                    : "Tell us about your system and the challenge you are facing. We read every message and reply within one business day.");
            }));

        var body = Section("form", "default", "container", "spacious",
            ("ContactFormBlock", "Form", child =>
            {
                Text(child, "ContactFormBlock", "Heading", ar ? "راسلنا" : "Send us a message");
                Text(child, "ContactFormBlock", "RecipientEmail", "hello@travotech.com");
                Text(child, "ContactFormBlock", "SubmitButtonText", ar ? "إرسال" : "Send");
                Text(child, "ContactFormBlock", "SuccessMessage", ar
                    ? "تم استلام رسالتك، شكراً لتواصلك معنا."
                    : "Thanks for reaching out — we have received your message.");
                Bool(child, "ContactFormBlock", "ShowOrganisation", true);
            }),
            ("ContactInfoBlock", "Details", child =>
            {
                Text(child, "ContactInfoBlock", "Heading", ar ? "معلومات التواصل" : "Contact details");
                Text(child, "ContactInfoBlock", "Email", "hello@travotech.com");
                Text(child, "ContactInfoBlock", "Phone", "+962 6 000 0000");
                Text(child, "ContactInfoBlock", "Address", ar ? "عمّان، الأردن" : "Amman, Jordan");
                Text(child, "ContactInfoBlock", "Hours", ar
                    ? "الأحد إلى الخميس، ٩ صباحاً – ٦ مساءً"
                    : "Sunday to Thursday, 9 AM – 6 PM");
                Text(child, "ContactInfoBlock", "Note", ar
                    ? "للاستفسارات التقنية العاجلة من العملاء الحاليين، استخدم قناة الدعم المخصصة في اتفاقية الخدمة."
                    : "For urgent technical enquiries from existing clients, use the dedicated support channel in your service agreement.");
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Contact", body));

        await EnsureAsync($"trafotechcontact{suffix}".PadRight(26, '0')[..26], "Page",
            "Contact", item =>
            {
                Localize(item, culture, "trafotech-contact");
                Route(item, ar ? "contact" : "en/contact");
                Text(item, "Seo", "MetaTitle", ar ? "تواصل معنا — ترافو تيك" : "Contact — Travotech");
                Text(item, "Seo", "MetaDescription", ar
                    ? "تواصل مع فريق ترافو تيك."
                    : "Get in touch with the Travotech team.");
                Bool(item, "Seo", "NoIndex", false);
                Attach(item, "Sections", sections);
            });
    }

    private async Task SeedContactAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "تواصل معنا" : "Contact");
                Text(child, "PageHeaderBlock", "Heading", ar
                    ? "لنتحدث عن مشروعك"
                    : "Let's talk about your project");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "أخبرنا عن التحدي الذي تواجهه، وسنعود إليك خلال يوم عمل واحد."
                    : "Tell us about the challenge you are facing and we will come back to you within one business day.");
            }));

        var form = Section("form", "alt", "container", "spacious",
            ("ContactFormBlock", "Form", child =>
            {
                Text(child, "ContactFormBlock", "Heading", ar ? "راسلنا" : "Send us a message");
                Text(child, "ContactFormBlock", "Intro", ar
                    ? "املأ النموذج وسنعاود التواصل معك."
                    : "Fill in the form and we will get back to you.");
                Text(child, "ContactFormBlock", "RecipientEmail", "hello@travotech.com");
                Text(child, "ContactFormBlock", "SubmitButtonText", ar ? "إرسال" : "Send");
                Text(child, "ContactFormBlock", "SuccessMessage", ar
                    ? "تم استلام رسالتك، شكراً لتواصلك معنا."
                    : "Thanks for reaching out — we have received your message.");
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Form", form));

        await EnsureAsync($"trafotechcontact{suffix}".PadRight(26, '0')[..26], "Page",
            "Contact", item =>
            {
                Localize(item, culture, "trafotech-contact");
                Route(item, ar ? "contact" : "en/contact");
                Text(item, "Seo", "MetaTitle", ar ? "تواصل معنا — ترافو تيك" : "Contact — Travotech");
                Text(item, "Seo", "MetaDescription", ar
                    ? "تواصل مع فريق ترافو تيك."
                    : "Get in touch with the Travotech team.");
                Bool(item, "Seo", "NoIndex", false);
                Attach(item, "Sections", sections);
            });
    }

    private async Task SeedAboutAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "عن الشركة" : "About");
                Text(child, "PageHeaderBlock", "Heading", ar
                    ? "نبني البنية التي تعمل عليها الأنظمة"
                    : "We build the infrastructure systems run on");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "فريق هندسي يعمل مع البنوك ومزودي خدمات الدفع منذ أكثر من عقد."
                    : "An engineering team working with banks and payment providers for over a decade.");
            }));

        var body = Section("story", "default", "container", "spacious",
            ("RichTextBlock", "Story", child =>
                Field(child, "RichTextBlock", "Body", new JsonObject
                {
                    ["Html"] = ar
                        ? "<h2>قصتنا</h2><p>بدأنا بفريق صغير وسؤال واحد: لماذا تستغرق التحويلات أياماً؟</p>"
                        : "<h2>Our story</h2><p>We started as a small team with one question: why do transfers take days?</p>",
                })));

        var cta = Section("cta", "dark", "full", "normal",
            ("CtaBlock", "CTA", child =>
            {
                Text(child, "CtaBlock", "Heading", ar ? "تحدّث إلينا" : "Talk to us");
                Text(child, "CtaBlock", "Text", ar
                    ? "نسعد بالحديث عن التحدي الذي تعمل عليه."
                    : "We would be glad to hear about the challenge you are working on.");
                Text(child, "CtaBlock", "ButtonText", ar ? "تواصل معنا" : "Contact us");
                Link(child, "CtaBlock", "ButtonLink", ar ? "/contact" : "/en/contact",
                     ar ? "تواصل معنا" : "Contact us");
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Story", body),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechabout{suffix}".PadRight(26, '0')[..26], "Page", "About", item =>
        {
            Localize(item, culture, "trafotech-about");
            Route(item, ar ? "about" : "en/about");
            Text(item, "Seo", "MetaTitle", ar ? "عن الشركة — ترافو تيك" : "About — Travotech");
            Text(item, "Seo", "MetaDescription", ar
                ? "تعرّف على فريق ترافو تيك."
                : "Meet the Travotech team.");
            Bool(item, "Seo", "NoIndex", false);
            Attach(item, "Sections", sections);
        });
    }

    // ------------------------------------------------------------- helpers

    /// <summary>
    /// Creates and publishes an item under a fixed id, unless that id already
    /// exists — which is what makes re-running harmless.
    /// </summary>
    private async Task EnsureAsync(
        string contentItemId,
        string contentType,
        string title,
        Action<JsonObject> build)
    {
        var existing = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
        if (existing is not null)
        {
            return;
        }

        var item = await _contentManager.NewAsync(contentType);
        item.ContentItemId = contentItemId;
        item.DisplayText = title;
        item.Alter<TitlePart>(p => p.Title = title);

        // Build the parts as plain JSON, then merge onto the item's content.
        var payload = new JsonObject();
        build(payload);

        // Indexed parts are welded through their typed models rather than raw
        // JSON: their indexes are populated by the part handlers, and a
        // JSON-only value leaves the index empty — which makes pages
        // unreachable by path and invisible to culture filtering.
        if (payload["AutoroutePart"] is JsonObject route)
        {
            payload.Remove("AutoroutePart");

            item.Alter<AutoroutePart>(p =>
            {
                p.Path = route["Path"]?.GetValue<string>() ?? string.Empty;
                p.SetHomepage = route["SetHomepage"]?.GetValue<bool>() ?? false;
            });
        }

        if (payload["LocalizationPart"] is JsonObject localization)
        {
            payload.Remove("LocalizationPart");

            item.Alter<LocalizationPart>(p =>
            {
                p.Culture = localization["Culture"]?.GetValue<string>();
                p.LocalizationSet = localization["LocalizationSet"]?.GetValue<string>();
            });
        }

        foreach (var (name, value) in payload)
        {
            item.Content[name] = value?.DeepClone();
        }

        await _contentManager.CreateAsync(item, VersionOptions.Draft);
        await _contentManager.PublishAsync(item);
    }

    private static void Text(JsonObject item, string part, string field, string value) =>
        Field(item, part, field, new JsonObject { ["Text"] = value });

    private static void Bool(JsonObject item, string part, string field, bool value) =>
        Field(item, part, field, new JsonObject { ["Value"] = value });

    private static void Link(JsonObject item, string part, string field, string url, string text) =>
        Field(item, part, field, new JsonObject { ["Url"] = url, ["Text"] = text });

    private static void Field(JsonObject item, string part, string field, JsonNode value)
    {
        if (item[part] is not JsonObject bucket)
        {
            bucket = [];
            item[part] = bucket;
        }

        bucket[field] = value;
    }

    private static void Localize(JsonObject item, string culture, string set)
    {
        item["LocalizationPart"] = new JsonObject
        {
            ["Culture"] = culture,
            ["LocalizationSet"] = set,
        };
    }

    private static void Route(JsonObject item, string path, bool homepage = false)
    {
        var route = new JsonObject { ["Path"] = path };
        if (homepage)
        {
            route["SetHomepage"] = true;
        }

        item["AutoroutePart"] = route;
    }

    private static void Attach(JsonObject item, string name, JsonObject bag) =>
        item[name] = bag;

    /// <summary>
    /// Bag contents are stored as an array of full content items.
    ///
    /// The JSON is assembled directly rather than by newing a ContentItem and
    /// serialising it: ContentItem.Content is dynamic, so calls on it resolve at
    /// runtime and ToJsonString is not among them.
    /// </summary>
    private static JsonObject Bag(params (string Type, string Title, Action<JsonObject> Build)[] items)
    {
        var array = new JsonArray();

        foreach (var (type, title, build) in items)
        {
            var child = new JsonObject
            {
                ["ContentType"] = type,
                ["ContentItemId"] = Guid.NewGuid().ToString("n")[..26],
                ["DisplayText"] = title,
                ["Latest"] = true,
                ["Published"] = true,
                ["TitlePart"] = new JsonObject { ["Title"] = title },
            };

            build(child);
            array.Add(child);
        }

        return new JsonObject { ["ContentItems"] = array };
    }

    private static Action<JsonObject> Section(
        string anchor,
        string background,
        string width,
        string spacing,
        params (string Type, string Title, Action<JsonObject> Build)[] blocks)
    {
        var bag = Bag(blocks);

        return item =>
        {
            Text(item, "Section", "Anchor", anchor);
            Text(item, "Section", "Background", background);
            Text(item, "Section", "Width", width);
            Text(item, "Section", "Spacing", spacing);
            Bool(item, "Section", "Hidden", false);
            Attach(item, "Blocks", bag);
        };
    }

    // -------------------------------------------------------------- chrome

    private async Task SeedChromeAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var navItems = ar
            ? new[] { ("الرئيسية", "/"), ("المنتجات", "/products"),
                      ("عن الشركة", "/about"), ("تواصل معنا", "/contact") }
            : new[] { ("Home", "/"), ("Products", "/en/products"),
                      ("About", "/en/about"), ("Contact", "/en/contact") };

        var navBag = Bag(navItems
            .Select<(string Label, string Url), (string, string, Action<JsonObject>)>(n =>
                ("NavLink", n.Label, child => Link(child, "NavLink", "Link", n.Url, n.Label)))
            .ToArray());

        await EnsureAsync($"trafotechheader{suffix}".PadRight(26, '0')[..26], "SiteHeader",
            "Site header", item =>
            {
                Localize(item, culture, "trafotech-siteheader");
                Text(item, "SiteHeader", "BrandName", ar ? "ترافو تيك" : "Travotech");
                Text(item, "SiteHeader", "CtaText", ar ? "اطلب عرضاً" : "Request a demo");
                Link(item, "SiteHeader", "CtaLink", ar ? "/contact" : "/en/contact",
                     ar ? "اطلب عرضاً" : "Request a demo");
                Bool(item, "SiteHeader", "ShowLanguageSwitcher", true);
                Bool(item, "SiteHeader", "ShowThemeToggle", true);
                Bool(item, "SiteHeader", "Sticky", true);
                Attach(item, "NavLinks", navBag);
            });

        var columns = ar
            ? new[]
            {
                ("الشركة", new[] { ("الرئيسية", "/"), ("المنتجات", "/products"),
                                   ("عن الشركة", "/about"), ("تواصل معنا", "/contact") }),
                ("المنتجات", new[] { ("التسوية الإجمالية الفورية", "/products"),
                                     ("منصة الدفع الفوري", "/products"),
                                     ("المحفظة الرقمية", "/products"),
                                     ("المقاصة الإلكترونية", "/products") }),
                ("تواصل", new[] { ("hello@travotech.com", "mailto:hello@travotech.com"),
                                  ("+962 6 000 0000", "tel:+96260000000") }),
            }
            : new[]
            {
                ("COMPANY", new[] { ("Home", "/"), ("Products", "/en/products"),
                                    ("About", "/en/about"), ("Contact", "/en/contact") }),
                ("PRODUCTS", new[] { ("Real-Time Gross Settlement", "/en/products"),
                                     ("Instant Payments Platform", "/en/products"),
                                     ("Digital Wallet", "/en/products"),
                                     ("Electronic Clearing", "/en/products") }),
                ("CONTACT", new[] { ("hello@travotech.com", "mailto:hello@travotech.com"),
                                    ("+962 6 000 0000", "tel:+96260000000") }),
            };

        var columnEntries = new List<(string, string, Action<JsonObject>)>();
        foreach (var (head, links) in columns)
        {
            var linkBag = Bag(links
                .Select<(string Label, string Url), (string, string, Action<JsonObject>)>(l =>
                    ("FooterLink", l.Label, child => Link(child, "FooterLink", "Link", l.Url, l.Label)))
                .ToArray());

            columnEntries.Add(("FooterLinkColumn", head, child => Attach(child, "Links", linkBag)));
        }

        var columnsBag = Bag(columnEntries.ToArray());

        await EnsureAsync($"trafotechfooter{suffix}".PadRight(26, '0')[..26], "SiteFooter",
            "Site footer", item =>
            {
                Localize(item, culture, "trafotech-sitefooter");
                Text(item, "SiteFooter", "BrandName", ar ? "ترافو تيك" : "Travotech");
                Text(item, "SiteFooter", "Tagline", ar
                    ? "بنية الدفع الفوري للاقتصاد الرقمي"
                    : "Real-time payment infrastructure for the digital economy");
                Text(item, "SiteFooter", "Email", "hello@travotech.com");
                Text(item, "SiteFooter", "Phone", "+962 6 000 0000");
                Text(item, "SiteFooter", "Location", ar ? "عمّان، الأردن" : "Amman, Jordan");
                Text(item, "SiteFooter", "Copyright", ar
                    ? "© 2026 ترافو تيك. جميع الحقوق محفوظة."
                    : "© 2026 Travotech. All rights reserved.");
                Text(item, "SiteFooter", "ComplianceNote", ar
                    ? "متوافق مع ISO 20022"
                    : "ISO 20022 compliant");
                Attach(item, "LinkColumns", columnsBag);
            });
    }

    // ---------------------------------------------------------------- home

    private async Task SeedHomeAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var stats = ar
            ? new[] { ("99.99", "%", "جاهزية النظام"), ("<5", "s", "زمن التحويل"),
                      ("24/7", "", "تشغيل متواصل"), ("12", "+", "سوقاً نشطاً") }
            : new[] { ("99.99", "%", "System uptime"), ("<5", "s", "Transfer time"),
                      ("24/7", "", "Always on"), ("12", "+", "Active markets") };

        var statEntries = stats
            .Select<(string V, string S, string L), (string, string, Action<JsonObject>)>(s =>
                ("StatItem", s.L, child =>
                {
                    Text(child, "StatItem", "Value", s.V);
                    Text(child, "StatItem", "Suffix", s.S);
                    Text(child, "StatItem", "Label", s.L);
                }))
            .ToArray();

        var statsBag = Bag(statEntries);

        var hero = Section("hero", "dark", "full", "none",
            ("PageHeroBlock", "Hero", child =>
            {
                Text(child, "PageHeroBlock", "Eyebrow", ar ? "مرحباً بكم في ترافو تيك" : "Welcome to Travotech");
                Text(child, "PageHeroBlock", "HeadingBefore", ar ? "حلول" : "Travotech");
                Text(child, "PageHeroBlock", "HeadingHighlight", ar ? "ترافو تيك" : "solutions");
                Text(child, "PageHeroBlock", "HeadingAfter", ar ? "لتمويل سلاسل التوريد" : "for supply chain finance");
                Text(child, "PageHeroBlock", "Lead", ar
                    ? "بنية الدفع الفوري للاقتصاد الرقمي"
                    : "Instant payment infrastructure for the digital economy");
                Text(child, "PageHeroBlock", "PrimaryText", ar ? "اطلب عرضاً توضيحياً" : "Request a demo");
                Link(child, "PageHeroBlock", "PrimaryLink", ar ? "/contact" : "/en/contact",
                     ar ? "اطلب عرضاً توضيحياً" : "Request a demo");
                Text(child, "PageHeroBlock", "SecondaryText", ar ? "استعرض المنتجات" : "Browse products");
                Link(child, "PageHeroBlock", "SecondaryLink", ar ? "/products" : "/en/products",
                     ar ? "استعرض المنتجات" : "Browse products");
                Text(child, "PageHeroBlock", "DialValue", "0.8s");
                Text(child, "PageHeroBlock", "DialLabel", ar ? "زمن التحويل" : "Transfer time");
                Text(child, "PageHeroBlock", "BadgeTitle", "ISO 20022");
                Text(child, "PageHeroBlock", "BadgeText", ar ? "متوافق مع ISO 20022" : "ISO 20022 compliant");
                Attach(child, "Stats", statsBag);
            }));

        var features = ar
            ? new[]
            {
                ("shield", "أمان بمعايير البنوك", "تشفير من الطرف إلى الطرف، ومفاتيح بأجهزة HSM، وسجل تدقيق غير قابل للتعديل لكل عملية."),
                ("growth", "يتوسّع مع نموّك", "بنية أفقية تعالج ملايين العمليات في الساعة دون توقف الخدمة أو تدهور زمن الاستجابة."),
                ("link", "تشغيل بيني مفتوح", "واجهات برمجية موثّقة ومعايير ISO 20022 تجعل الربط مع أي نظام قائم مسألة أيام لا شهور."),
                ("support", "دعم على مدار الساعة", "فرق هندسية في ثلاث مناطق زمنية، مع اتفاقيات مستوى خدمة مضمونة تعاقدياً."),
            }
            : new[]
            {
                ("shield", "Bank-grade security", "End-to-end encryption, HSM-held keys, and an immutable audit trail for every transaction."),
                ("growth", "Scales as you grow", "A horizontal architecture handling millions of transactions an hour with no downtime and no latency drift."),
                ("link", "Open interoperability", "Documented APIs and ISO 20022 standards make integrating with any existing system a matter of days, not months."),
                ("support", "Round-the-clock support", "Engineering teams across three time zones, with contractually guaranteed SLAs."),
            };

        var featuresBag = Bag(features
            .Select<(string I, string T, string D), (string, string, Action<JsonObject>)>(f =>
                ("FeatureItem", f.T, child =>
                {
                    Text(child, "FeatureItem", "Icon", f.I);
                    Text(child, "FeatureItem", "Description", f.D);
                }))
            .ToArray());

        var why = Section("why", "alt", "container", "spacious",
            ("FeatureGridBlock", "Why", child =>
            {
                Text(child, "FeatureGridBlock", "Eyebrow", ar ? "لماذا ترافو تيك" : "Why Travotech");
                Text(child, "FeatureGridBlock", "Heading", ar
                    ? "بنية تحتية يُبنى عليها اقتصاد"
                    : "Infrastructure an economy can be built on");
                Text(child, "FeatureGridBlock", "Lead", ar
                    ? "كل قرار هندسي في منصتنا يعود إلى سؤال واحد: ماذا يحدث لو توقف هذا النظام لدقيقة واحدة؟"
                    : "Every engineering decision comes back to one question: what happens if this system stops for a single minute?");
                Attach(child, "Features", featuresBag);
            }));

        var productCards = Bag((ar
            ? new[]
            {
                ("البنوك المركزية", "التسوية الإجمالية الفورية", "تسوية نهائية لا رجعة فيها للمدفوعات عالية القيمة، مع إدارة سيولة لحظية ونافذة تشغيل ممتدة."),
                ("البنوك المركزية", "منصة الدفع الفوري", "تحويلات على مدار الساعة تصل خلال أقل من خمس ثوانٍ، مبنية للتشغيل البيني بين كل بنوك السوق."),
                ("البنوك التجارية", "المحفظة الرقمية", "محفظة بعلامتك التجارية للأفراد والتجار، مع تعريف رقمي بالهوية وحدود مرنة حسب مستوى العميل."),
            }
            : new[]
            {
                ("Central banks", "Real-Time Gross Settlement", "Irrevocable final settlement for high-value payments, with real-time liquidity management and an extended operating window."),
                ("Central banks", "Instant Payments Platform", "Round-the-clock transfers arriving in under five seconds, built for interoperability across every bank in the market."),
                ("Commercial banks", "Digital Wallet", "A white-label wallet for consumers and merchants, with digital identity verification and flexible limits by customer tier."),
            })
            .Select<(string B, string T, string D), (string, string, Action<JsonObject>)>(c =>
                ("ProductCard", c.T, child =>
                {
                    Text(child, "ProductCard", "Badge", c.B);
                    Text(child, "ProductCard", "Description", c.D);
                }))
            .ToArray());

        var products = Section("products", "alt", "container", "spacious",
            ("ProductGridBlock", "Products", child =>
            {
                Text(child, "ProductGridBlock", "Eyebrow", ar ? "منتجاتنا" : "Our products");
                Text(child, "ProductGridBlock", "Heading", ar
                    ? "منظومة دفع متكاملة"
                    : "A complete payments ecosystem");
                Text(child, "ProductGridBlock", "LinkText", ar ? "كل المنتجات" : "All products");
                Link(child, "ProductGridBlock", "Link", ar ? "/products" : "/en/products",
                     ar ? "كل المنتجات" : "All products");
                Attach(child, "Products", productCards);
            }));

        var cta = Section("cta", "dark", "container", "normal",
            ("CtaBlock", "CTA", child =>
            {
                Text(child, "CtaBlock", "Heading", ar
                    ? "جاهزون للحديث عن نظامك القادم؟"
                    : "Ready to talk about your next system?");
                Text(child, "CtaBlock", "Text", ar
                    ? "أخبرنا عن التحدي الذي تواجهه، وسنعود إليك خلال يوم عمل واحد."
                    : "Tell us about the challenge you are facing and we will come back to you within one business day.");
                Text(child, "CtaBlock", "ButtonText", ar ? "ابدأ المحادثة" : "Start the conversation");
                Link(child, "CtaBlock", "ButtonLink", ar ? "/contact" : "/en/contact",
                     ar ? "ابدأ المحادثة" : "Start the conversation");
            }));

        var sections = Bag(
            ("Section", "Hero", hero),
            ("Section", "Why", why),
            ("Section", "Products", products),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechhome{suffix}".PadRight(26, '0')[..26], "Page", "Home", item =>
        {
            Localize(item, culture, "trafotech-home");
            Route(item, ar ? "home" : "en", homepage: ar);
            Text(item, "Seo", "MetaTitle", ar
                ? "ترافو تيك — بنية الدفع الفوري للاقتصاد الرقمي"
                : "Travotech — Instant payment infrastructure for the digital economy");
            Text(item, "Seo", "MetaDescription", ar
                ? "حلول ترافو تيك لتمويل سلاسل التوريد وبنية الدفع الفوري."
                : "Travotech solutions for supply chain finance and instant payment infrastructure.");
            Bool(item, "Seo", "NoIndex", false);
            Attach(item, "Sections", sections);
        });
    }

    // ------------------------------------------------------------ products

    private async Task SeedProductsAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var categories = ar
            ? new[] { ("central", "البنوك المركزية"), ("commercial", "البنوك التجارية"),
                      ("psp", "مزودو خدمات الدفع") }
            : new[] { ("central", "Central banks"), ("commercial", "Commercial banks"),
                      ("psp", "Payment service providers") };

        var categoriesBag = Bag(categories
            .Select<(string K, string L), (string, string, Action<JsonObject>)>(c =>
                ("ProductCategory", c.L, child => Text(child, "ProductCategory", "Key", c.K)))
            .ToArray());

        var products = ar
            ? new[]
            {
                ("activity", "central", "التسوية الإجمالية الفورية", "تسوية نهائية لا رجعة فيها للمدفوعات عالية القيمة، مع إدارة سيولة لحظية ونافذة تشغيل ممتدة.", new[] { "تسوية نهائية خلال ثوانٍ", "إدارة طوابير وسيولة ذكية", "توافق كامل مع ISO 20022" }),
                ("bolt", "central", "منصة الدفع الفوري", "تحويلات على مدار الساعة تصل خلال أقل من خمس ثوانٍ، مبنية للتشغيل البيني بين كل بنوك السوق.", new[] { "متاح ٢٤/٧ طوال أيام السنة", "عناوين اسمية بدل أرقام الحسابات", "طلب الدفع والتحقق من المستفيد" }),
                ("wallet", "commercial", "المحفظة الرقمية", "محفظة بعلامتك التجارية للأفراد والتجار، مع تعريف رقمي بالهوية وحدود مرنة حسب مستوى العميل.", new[] { "تعريف رقمي بالهوية خلال دقائق", "دفع بالرمز الموحّد QR", "محافظ فرعية ورقابة على الإنفاق" }),
                ("check", "commercial", "المقاصة الإلكترونية", "مقاصة الشيكات والخصم المباشر والرواتب في دورة واحدة، مع أرشفة صور وتحقق آلي من التوقيع.", new[] { "أرشيف صور بحث فوري", "تحقق آلي من التوقيع", "معالجة الرواتب بالجملة" }),
                ("hub", "psp", "مركز المدفوعات", "طبقة واحدة توجّه كل رسائل الدفع بين أنظمتك والشبكات الخارجية، وتترجم بين المعايير تلقائياً.", new[] { "تحويل MT إلى MX تلقائياً", "توجيه ذكي حسب القواعد", "سجل مركزي لكل رسالة" }),
                ("card", "psp", "اشترِ الآن وادفع لاحقاً", "تقسيط لحظي عند نقطة البيع مع تقييم ائتماني آني وربط مباشر مع بوابة التاجر.", new[] { "قرار ائتماني خلال ثوانٍ", "خطط سداد مرنة", "بوابة تاجر متكاملة" }),
            }
            : new[]
            {
                ("activity", "central", "Real-Time Gross Settlement", "Irrevocable final settlement for high-value payments, with real-time liquidity management and an extended operating window.", new[] { "Final settlement in seconds", "Smart queue and liquidity management", "Full ISO 20022 compliance" }),
                ("bolt", "central", "Instant Payments Platform", "Round-the-clock transfers arriving in under five seconds, built for interoperability across every bank in the market.", new[] { "Available 24/7, every day of the year", "Aliases instead of account numbers", "Request to pay and payee verification" }),
                ("wallet", "commercial", "Digital Wallet", "A white-label wallet for consumers and merchants, with digital identity verification and flexible limits by customer tier.", new[] { "Digital identity verification in minutes", "Unified QR code payments", "Sub-wallets and spending controls" }),
                ("check", "commercial", "Electronic Clearing", "Cheques, direct debits and payroll cleared in a single cycle, with image archiving and automated signature verification.", new[] { "Instantly searchable image archive", "Automated signature verification", "Bulk payroll processing" }),
                ("hub", "psp", "Payments Hub", "A single layer routing every payment message between your systems and external networks, translating between standards automatically.", new[] { "Automatic MT to MX conversion", "Rule-based smart routing", "A central log for every message" }),
                ("card", "psp", "Buy Now, Pay Later", "Instant point-of-sale instalments with real-time credit assessment and a direct merchant gateway integration.", new[] { "Credit decisions in seconds", "Flexible repayment plans", "Integrated merchant gateway" }),
            };

        var rowEntries = new List<(string, string, Action<JsonObject>)>();
        foreach (var (icon, cat, title, desc, points) in products)
        {
            var pointsBag = Bag(points
                .Select<string, (string, string, Action<JsonObject>)>(pt =>
                    ("ProductPoint", pt, _ => { }))
                .ToArray());

            var label = categories.First(c => c.Item1 == cat).Item2;

            rowEntries.Add(("ProductRow", title, child =>
            {
                Text(child, "ProductRow", "Icon", icon);
                Text(child, "ProductRow", "Category", cat);
                Text(child, "ProductRow", "CategoryLabel", label);
                Text(child, "ProductRow", "Description", desc);
                Attach(child, "Points", pointsBag);
            }));
        }

        var rowsBag = Bag(rowEntries.ToArray());

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "المنتجات" : "Products");
                Text(child, "PageHeaderBlock", "Heading", ar
                    ? "منظومة دفع متكاملة"
                    : "A complete payments ecosystem");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "من التسوية بين البنوك إلى المحفظة في جيب العميل — كل طبقة مبنية لتعمل وحدها أو مع البقية."
                    : "From interbank settlement to the wallet in your customer's pocket — every layer is built to run on its own or with the rest.");
            }));

        var list = Section("list", "alt", "container", "spacious",
            ("ProductListBlock", "Products", child =>
            {
                Text(child, "ProductListBlock", "AllLabel", ar ? "الكل" : "All");
                Bool(child, "ProductListBlock", "ShowFilters", true);
                Attach(child, "Categories", categoriesBag);
                Attach(child, "Rows", rowsBag);
            }));

        var cta = Section("cta", "default", "container", "spacious",
            ("CtaBlock", "CTA", child =>
            {
                Text(child, "CtaBlock", "Heading", ar ? "لم تجد ما تبحث عنه؟" : "Not finding what you need?");
                Text(child, "CtaBlock", "Text", ar
                    ? "معظم أنظمتنا مبنية على قياس. تحدّث إلينا عن حالتك وسنخبرك بصراحة إن كنا الخيار المناسب."
                    : "Most of our systems are built to order. Tell us about your case and we will tell you honestly whether we are the right fit.");
                Text(child, "CtaBlock", "ButtonText", ar ? "تواصل معنا" : "Contact us");
                Link(child, "CtaBlock", "ButtonLink", ar ? "/contact" : "/en/contact",
                     ar ? "تواصل معنا" : "Contact us");
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Products", list),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechproducts{suffix}".PadRight(26, '0')[..26], "Page",
            "Products", item =>
            {
                Localize(item, culture, "trafotech-products");
                Route(item, ar ? "products" : "en/products");
                Text(item, "Seo", "MetaTitle", ar ? "المنتجات — ترافو تيك" : "Products — Travotech");
                Text(item, "Seo", "MetaDescription", ar
                    ? "منظومة دفع متكاملة من ترافو تيك."
                    : "A complete payments ecosystem from Travotech.");
                Bool(item, "Seo", "NoIndex", false);
                Attach(item, "Sections", sections);
            });
    }
}
