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

    // ================================================================
    // Software-house repositioning.
    //
    // The site was seeded as a payments-product company. This migration
    // replaces every seeded page and the header/footer with content for a
    // general software development company (custom web, mobile and cloud
    // work). The old pages are still exactly as seeded — an editor's own
    // changes would have been kept by EnsureAsync — so they are removed and
    // recreated. Chrome is recreated too, then its logos are re-applied,
    // because UpdateFrom12 (which set them once) will not run again.
    //
    // Runs after the gallery migration (v15/16), so it is UpdateFrom16.
    // ================================================================
    public async Task<int> UpdateFrom16Async()
    {
        foreach (var id in new[]
                 {
                     "trafotechhome0000000000000", "trafotechhomeen00000000000",
                     "trafotechproducts000000000", "trafotechproductsen0000000",
                     "trafotechabout000000000000", "trafotechabouten0000000000",
                     "trafotechcontact0000000000", "trafotechcontacten00000000",
                     "trafotechheader00000000000", "trafotechheaderen000000000",
                     "trafotechfooter00000000000", "trafotechfooteren000000000",
                 })
        {
            var existing = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (existing is not null)
            {
                await _contentManager.RemoveAsync(existing);
            }
        }

        await SeedChromeSoftwareAsync("ar");
        await SeedChromeSoftwareAsync("en");
        await SeedHomeSoftwareAsync("ar");
        await SeedHomeSoftwareAsync("en");
        await SeedServicesSoftwareAsync("ar");
        await SeedServicesSoftwareAsync("en");
        await SeedAboutSoftwareAsync("ar");
        await SeedAboutSoftwareAsync("en");
        await SeedContactSoftwareAsync("ar");
        await SeedContactSoftwareAsync("en");

        // UpdateFrom12 set these once and will not run again, so re-apply.
        foreach (var id in new[] { "trafotechheader00000000000", "trafotechheaderen000000000" })
        {
            await SetLogoAsync(id, "SiteHeader", "travotech-logo-new.png");
        }

        foreach (var id in new[] { "trafotechfooter00000000000", "trafotechfooteren000000000" })
        {
            await SetLogoAsync(id, "SiteFooter", "travotech-logo-light.png");
        }

        return 17;
    }

    // ================================================================
    // Enriched content: solution detail pages + extra sections.
    //
    // UpdateFrom16 reseeded the base pages. This migration deletes them
    // again (so the enriched versions are recreated) and adds three new
    // solution detail pages reachable from the home page product cards.
    // ================================================================
    public async Task<int> UpdateFrom17Async()
    {
        // Remove the pages seeded by UpdateFrom16 so the enriched
        // versions (with links, FAQ, capabilities, etc.) are recreated.
        foreach (var id in new[]
                 {
                     "trafotechhome0000000000000", "trafotechhomeen00000000000",
                     "trafotechproducts000000000", "trafotechproductsen0000000",
                     "trafotechabout000000000000", "trafotechabouten0000000000",
                     "trafotechcontact0000000000", "trafotechcontacten00000000",
                     "trafotechheader00000000000", "trafotechheaderen000000000",
                     "trafotechfooter00000000000", "trafotechfooteren000000000",
                 })
        {
            var existing = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (existing is not null)
            {
                await _contentManager.RemoveAsync(existing);
            }
        }

        await SeedChromeSoftwareAsync("ar");
        await SeedChromeSoftwareAsync("en");
        await SeedHomeSoftwareAsync("ar");
        await SeedHomeSoftwareAsync("en");
        await SeedServicesSoftwareAsync("ar");
        await SeedServicesSoftwareAsync("en");
        await SeedAboutSoftwareAsync("ar");
        await SeedAboutSoftwareAsync("en");
        await SeedContactSoftwareAsync("ar");
        await SeedContactSoftwareAsync("en");

        // New solution detail pages.
        await SeedSolutionWebAsync("ar");
        await SeedSolutionWebAsync("en");
        await SeedSolutionMobileAsync("ar");
        await SeedSolutionMobileAsync("en");
        await SeedSolutionCloudAsync("ar");
        await SeedSolutionCloudAsync("en");

        // Re-apply logos (UpdateFrom12 won't run again).
        foreach (var id in new[] { "trafotechheader00000000000", "trafotechheaderen000000000" })
        {
            await SetLogoAsync(id, "SiteHeader", "travotech-logo-new.png");
        }

        foreach (var id in new[] { "trafotechfooter00000000000", "trafotechfooteren000000000" })
        {
            await SetLogoAsync(id, "SiteFooter", "travotech-logo-light.png");
        }

        return 18;
    }

    // ------------------------------------------------ solution: web -------

    private async Task SeedSolutionWebAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var features = ar
            ? new[]
            {
                ("code", "واجهات حديثة", "مكوّنات قابلة لإعادة الاستخدام بـ Angular و React مع حالة مُدارة بكفاءة."),
                ("gauge", "أداء عالٍ", "تحميل أولي أقل من ثانيتين، تقسيم الكود، وتخزين مؤقت ذكي."),
                ("shield", "آمن افتراضياً", "حماية CSRF و XSS، مصادقة OAuth2/OpenID Connect، وصلاحيات حسب الدور."),
                ("phone", "متجاوب بالكامل", "تصميم يبدو ممتازاً من 320px إلى 4K، مع اختبار على أجهزة حقيقية."),
            }
            : new[]
            {
                ("code", "Modern UI", "Reusable components in Angular and React with efficient state management."),
                ("gauge", "High Performance", "Sub-2s initial load, code splitting, and smart caching."),
                ("shield", "Secure by Default", "CSRF and XSS protection, OAuth2/OpenID Connect auth, role-based access."),
                ("phone", "Fully Responsive", "Design that looks great from 320px to 4K, tested on real devices."),
            };

        var featuresBag = Bag(features
            .Select<(string I, string T, string D), (string, string, Action<JsonObject>)>(f =>
                ("FeatureItem", f.T, child =>
                {
                    Text(child, "FeatureItem", "Icon", f.I);
                    Text(child, "FeatureItem", "Description", f.D);
                }))
            .ToArray());

        var techCards = Bag((ar
            ? new[]
            {
                ("Angular", "إطار عمل قوي للمؤسسات", new[] { "TypeScript", "RxJS", "NgRx", "Material" }),
                ("React", "مرونة وبيئة ضخمة", new[] { "Hooks", "Next.js", "Redux", "Tailwind" }),
                (".NET", "خوادم سريعة وموثوقة", new[] { "ASP.NET Core", "SignalR", "EF Core", "gRPC" }),
            }
            : new[]
            {
                ("Angular", "Powerful enterprise framework", new[] { "TypeScript", "RxJS", "NgRx", "Material" }),
                ("React", "Flexibility & huge ecosystem", new[] { "Hooks", "Next.js", "Redux", "Tailwind" }),
                (".NET", "Fast, reliable backend", new[] { "ASP.NET Core", "SignalR", "EF Core", "gRPC" }),
            })
            .Select<(string B, string T, string[] P), (string, string, Action<JsonObject>)>(c =>
                ("ProductCard", c.T, child =>
                {
                    Text(child, "ProductCard", "Badge", c.B);
                    Text(child, "ProductCard", "Description", c.T);
                    Attach(child, "Points", Bag(c.P
                        .Select<string, (string, string, Action<JsonObject>)>(pt =>
                            ("ProductPoint", pt, _ => { }))
                        .ToArray()));
                }))
            .ToArray());

        var faqItems = ar
            ? new[]
            {
                ("هل تبنيون تطبيقات ويب تقدمية (PWA)؟", "نعم، نبني PWA تعمل دون اتصال ويمكن تثبيتها على الجهاز مع إشعارات فورية."),
                ("هل يمكنكم تحسين تطبيق ويب موجود؟", "بالتأكيد. نحلل الأداء والبنية ونحسّنها تدريجياً دون إعادة كتابة كل شيء."),
            }
            : new[]
            {
                ("Do you build Progressive Web Apps (PWA)?", "Yes, we build PWAs that work offline, can be installed on the device, and support push notifications."),
                ("Can you improve an existing web app?", "Absolutely. We analyse performance and architecture and improve it incrementally without rewriting everything."),
            };

        var faqBag = Bag(faqItems
            .Select<(string Q, string A), (string, string, Action<JsonObject>)>(f =>
                ("FaqItem", f.Q, child =>
                    Field(child, "FaqItem", "Answer", new JsonObject { ["Html"] = $"<p>{f.A}</p>" })))
            .ToArray());

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "الحلول" : "Solutions");
                Text(child, "PageHeaderBlock", "Heading", ar
                    ? "تطوير تطبيقات الويب"
                    : "Web Application Development");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "تطبيقات ويب سريعة وآمنة وقابلة للتوسّع، مبنية بأحدث الأطر وأفضل الممارسات."
                    : "Fast, secure and scalable web apps built with modern frameworks and best practices.");
            }));

        var body = Section("body", "default", "container", "spacious",
            ("RichTextBlock", "Overview", child =>
            {
                Field(child, "RichTextBlock", "Body", new JsonObject
                {
                    ["Html"] = ar
                        ? "<p>نبني تطبيقات ويب من البداية أو نطوّر أنظمة موجودة. نبدأ بفهم متطلباتك وجمهورك، ثم نصمّم البنية ونكتب الكود بأحدث الأطر.</p>"
                          + "<p>كل مشروع يمر بمرحلة اكتشاف، تصميم، تطوير بسبرنتات، واختبار قبل الإطلاق. والنتيجة: تطبيق يعمل بسلاسة على كل المتصفحات والأجهزة.</p>"
                        : "<p>We build web apps from scratch or evolve existing systems. We start by understanding your requirements and audience, then design the architecture and write code with modern frameworks.</p>"
                          + "<p>Every project goes through discovery, design, sprint-based development, and testing before launch. The result: an app that works smoothly on every browser and device.</p>",
                });
            }));

        var featuresSection = Section("features", "alt", "container", "spacious",
            ("FeatureGridBlock", "Features", child =>
            {
                Text(child, "FeatureGridBlock", "Eyebrow", ar ? "المميزات" : "Features");
                Text(child, "FeatureGridBlock", "Heading", ar
                    ? "ما الذي تحصل عليه"
                    : "What you get");
                Attach(child, "Features", featuresBag);
            }));

        var techSection = Section("tech", "default", "container", "spacious",
            ("ProductGridBlock", "Tech", child =>
            {
                Text(child, "ProductGridBlock", "Eyebrow", ar ? "التقنيات" : "Technologies");
                Text(child, "ProductGridBlock", "Heading", ar
                    ? "أدوات نستخدمها"
                    : "Tools we use");
                Attach(child, "Products", techCards);
            }));

        var faqSection = Section("faq", "alt", "container", "spacious",
            ("FaqBlock", "FAQ", child =>
            {
                Text(child, "FaqBlock", "Heading", ar ? "أسئلة شائعة" : "FAQ");
                Attach(child, "Questions", faqBag);
            }));

        var cta = Section("cta", "dark", "container", "normal",
            ("CtaBlock", "CTA", child =>
            {
                Text(child, "CtaBlock", "Heading", ar
                    ? "جاهز تبدأ مشروع ويب؟"
                    : "Ready to start a web project?");
                Text(child, "CtaBlock", "Text", ar
                    ? "احكِ لنا عن فكرتك وسنعود إليك بخطة خلال يوم عمل واحد."
                    : "Tell us about your idea and we will come back with a plan within one business day.");
                Text(child, "CtaBlock", "ButtonText", ar ? "ابدأ المحادثة" : "Start the conversation");
                Link(child, "CtaBlock", "ButtonLink", ar ? "/contact" : "/en/contact",
                     ar ? "ابدأ المحادثة" : "Start the conversation");
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Body", body),
            ("Section", "Features", featuresSection),
            ("Section", "Tech", techSection),
            ("Section", "FAQ", faqSection),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechsolvweb{suffix}".PadRight(26, '0')[..26], "Page",
            ar ? "تطوير الويب" : "Web Development", item =>
            {
                Localize(item, culture, "trafotech-sol-web");
                Route(item, ar ? "solutions/web" : "en/solutions/web");
                Text(item, "Seo", "MetaTitle", ar
                    ? "تطوير تطبيقات الويب — ترافو تيك"
                    : "Web Application Development — Travotech");
                Text(item, "Seo", "MetaDescription", ar
                    ? "نبني تطبيقات ويب سريعة وآمنة وقابلة للتوسّع بـ Angular و React و .NET."
                    : "We build fast, secure and scalable web apps with Angular, React and .NET.");
                Bool(item, "Seo", "NoIndex", false);
                Attach(item, "Sections", sections);
            });
    }

    // ------------------------------------------------ solution: mobile ----

    private async Task SeedSolutionMobileAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var features = ar
            ? new[]
            {
                ("phone", "قاعدة كود واحدة", "تطبيقات iOS وأندرويد من كود واحد بـ Flutter أو React Native."),
                ("wifi-off", "دعم دون اتصال", "بيانات مخزّنة محلياً ومزامنة تلقائية عند عودة الاتصال."),
                ("bell", "إشعارات فورية", "إشعارات Push عبر Firebase أو APNS لتفاعل أعلى مع المستخدم."),
                ("shield", "أمان البيانات", "تشفير محلي، مصادقة بيومترية، وتخزين آمن للرموز الحساسة."),
            }
            : new[]
            {
                ("phone", "Single Codebase", "iOS and Android apps from one codebase with Flutter or React Native."),
                ("wifi-off", "Offline Support", "Locally cached data with automatic sync when connectivity returns."),
                ("bell", "Push Notifications", "Push notifications via Firebase or APNS for higher user engagement."),
                ("shield", "Data Security", "Local encryption, biometric authentication, and secure storage for sensitive tokens."),
            };

        var featuresBag = Bag(features
            .Select<(string I, string T, string D), (string, string, Action<JsonObject>)>(f =>
                ("FeatureItem", f.T, child =>
                {
                    Text(child, "FeatureItem", "Icon", f.I);
                    Text(child, "FeatureItem", "Description", f.D);
                }))
            .ToArray());

        var techCards = Bag((ar
            ? new[]
            {
                ("Flutter", "أداء أصلي من كود واحد", new[] { "Dart", "Skia", "Firebase", "Riverpod" }),
                ("React Native", "بيئة JavaScript ضخمة", new[] { "TypeScript", "Expo", "Redux", "Reanimated" }),
                ("Swift / Kotlin", "أصلي 100% عند الحاجة", new[] { "SwiftUI", "CoreData", "Jetpack", "Room" }),
            }
            : new[]
            {
                ("Flutter", "Native performance from one code", new[] { "Dart", "Skia", "Firebase", "Riverpod" }),
                ("React Native", "Huge JavaScript ecosystem", new[] { "TypeScript", "Expo", "Redux", "Reanimated" }),
                ("Swift / Kotlin", "100% native when needed", new[] { "SwiftUI", "CoreData", "Jetpack", "Room" }),
            })
            .Select<(string B, string T, string[] P), (string, string, Action<JsonObject>)>(c =>
                ("ProductCard", c.T, child =>
                {
                    Text(child, "ProductCard", "Badge", c.B);
                    Text(child, "ProductCard", "Description", c.T);
                    Attach(child, "Points", Bag(c.P
                        .Select<string, (string, string, Action<JsonObject>)>(pt =>
                            ("ProductPoint", pt, _ => { }))
                        .ToArray()));
                }))
            .ToArray());

        var faqItems = ar
            ? new[]
            {
                ("هل تنشرون على المتاجر؟", "نعم، نتعامل مع عملية النشر على App Store و Google Play بالكامل بما في ذلك المراجعة والتصحيح."),
                ("هل يمكن لتطبيق الموبايل التحدث مع نظامنا الحالي؟", "نعم، نربط التطبيق بواجهاتكم البرمجية الحالية أو نبني واجهات جديدة حسب الحاجة."),
            }
            : new[]
            {
                ("Do you publish to the app stores?", "Yes, we handle the full publishing process on the App Store and Google Play including review and fixes."),
                ("Can the mobile app talk to our existing system?", "Yes, we connect the app to your existing APIs or build new ones as needed."),
            };

        var faqBag = Bag(faqItems
            .Select<(string Q, string A), (string, string, Action<JsonObject>)>(f =>
                ("FaqItem", f.Q, child =>
                    Field(child, "FaqItem", "Answer", new JsonObject { ["Html"] = $"<p>{f.A}</p>" })))
            .ToArray());

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "الحلول" : "Solutions");
                Text(child, "PageHeaderBlock", "Heading", ar
                    ? "تطوير تطبيقات الموبايل"
                    : "Mobile App Development");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "تطبيقات iOS وأندرويد بجودة أصلية من قاعدة كود واحدة، مع دعم دون اتصال وإشعارات."
                    : "Native-quality iOS and Android apps from a single codebase, with offline support and notifications.");
            }));

        var body = Section("body", "default", "container", "spacious",
            ("RichTextBlock", "Overview", child =>
            {
                Field(child, "RichTextBlock", "Body", new JsonObject
                {
                    ["Html"] = ar
                        ? "<p>نبني تطبيقات موبايل تعمل بسلاسة على iOS وأندرويد من قاعدة كود واحدة. نستخدم Flutter أو React Native لمعظم المشاريع، ونلجأ للكود الأصلي عند الحاجة لميزات متقدمة.</p>"
                          + "<p>نهتم بتجربة المستخدم والأداء واستهلاك البطارية، ونختبر على أجهزة حقيقية قبل النشر.</p>"
                        : "<p>We build mobile apps that run smoothly on iOS and Android from a single codebase. We use Flutter or React Native for most projects, and drop to native code when advanced features require it.</p>"
                          + "<p>We care about UX, performance and battery usage, and test on real devices before publishing.</p>",
                });
            }));

        var featuresSection = Section("features", "alt", "container", "spacious",
            ("FeatureGridBlock", "Features", child =>
            {
                Text(child, "FeatureGridBlock", "Eyebrow", ar ? "المميزات" : "Features");
                Text(child, "FeatureGridBlock", "Heading", ar ? "ما الذي تحصل عليه" : "What you get");
                Attach(child, "Features", featuresBag);
            }));

        var techSection = Section("tech", "default", "container", "spacious",
            ("ProductGridBlock", "Tech", child =>
            {
                Text(child, "ProductGridBlock", "Eyebrow", ar ? "التقنيات" : "Technologies");
                Text(child, "ProductGridBlock", "Heading", ar ? "أدوات نستخدمها" : "Tools we use");
                Attach(child, "Products", techCards);
            }));

        var faqSection = Section("faq", "alt", "container", "spacious",
            ("FaqBlock", "FAQ", child =>
            {
                Text(child, "FaqBlock", "Heading", ar ? "أسئلة شائعة" : "FAQ");
                Attach(child, "Questions", faqBag);
            }));

        var cta = Section("cta", "dark", "container", "normal",
            ("CtaBlock", "CTA", child =>
            {
                Text(child, "CtaBlock", "Heading", ar
                    ? "جاهز تبدأ تطبيق موبايل؟"
                    : "Ready to start a mobile app?");
                Text(child, "CtaBlock", "Text", ar
                    ? "احكِ لنا عن فكرتك وسنعود إليك بخطة خلال يوم عمل واحد."
                    : "Tell us about your idea and we will come back with a plan within one business day.");
                Text(child, "CtaBlock", "ButtonText", ar ? "ابدأ المحادثة" : "Start the conversation");
                Link(child, "CtaBlock", "ButtonLink", ar ? "/contact" : "/en/contact",
                     ar ? "ابدأ المحادثة" : "Start the conversation");
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Body", body),
            ("Section", "Features", featuresSection),
            ("Section", "Tech", techSection),
            ("Section", "FAQ", faqSection),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechsolvmob{suffix}".PadRight(26, '0')[..26], "Page",
            ar ? "تطبيقات الموبايل" : "Mobile Apps", item =>
            {
                Localize(item, culture, "trafotech-sol-mobile");
                Route(item, ar ? "solutions/mobile" : "en/solutions/mobile");
                Text(item, "Seo", "MetaTitle", ar
                    ? "تطوير تطبيقات الموبايل — ترافو تيك"
                    : "Mobile App Development — Travotech");
                Text(item, "Seo", "MetaDescription", ar
                    ? "تطبيقات iOS وأندرويد من قاعدة كود واحدة بـ Flutter و React Native."
                    : "iOS and Android apps from a single codebase with Flutter and React Native.");
                Bool(item, "Seo", "NoIndex", false);
                Attach(item, "Sections", sections);
            });
    }

    // ------------------------------------------------ solution: cloud -----

    private async Task SeedSolutionCloudAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var features = ar
            ? new[]
            {
                ("rocket", "نشر مؤتمت", "خطوط CI/CD تبني وتختبر وتنشر تلقائياً عند كل دمج."),
                ("container", "حاويات", "Docker و Kubernetes لتوسّع أفقي وسهل دون توقف."),
                ("gauge", "مراقبة وتنبيهات", "لوحات أداء وتنبيهات فورية عند أي مشكلة في الإنتاج."),
                ("shield", "أمان سحابي", "شبكات معزولة، إدارة أسرار، وامتثال للمعايير."),
            }
            : new[]
            {
                ("rocket", "Automated Deployment", "CI/CD pipelines that build, test and deploy automatically on every merge."),
                ("container", "Containers", "Docker and Kubernetes for horizontal scaling with zero downtime."),
                ("gauge", "Monitoring & Alerts", "Performance dashboards and instant alerts on any production issue."),
                ("shield", "Cloud Security", "Isolated networks, secrets management, and standards compliance."),
            };

        var featuresBag = Bag(features
            .Select<(string I, string T, string D), (string, string, Action<JsonObject>)>(f =>
                ("FeatureItem", f.T, child =>
                {
                    Text(child, "FeatureItem", "Icon", f.I);
                    Text(child, "FeatureItem", "Description", f.D);
                }))
            .ToArray());

        var techCards = Bag((ar
            ? new[]
            {
                ("Azure", "السحابة الأولى للمؤسسات", new[] { "App Service", "Functions", "AKS", "Blob Storage" }),
                ("AWS", "مرونة وخدمات لا تنتهي", new[] { "ECS", "Lambda", "RDS", "S3" }),
                ("DevOps", "أتمتة كاملة", new[] { "GitHub Actions", "Terraform", "Docker", "Helm" }),
            }
            : new[]
            {
                ("Azure", "Enterprise-first cloud", new[] { "App Service", "Functions", "AKS", "Blob Storage" }),
                ("AWS", "Flexibility & endless services", new[] { "ECS", "Lambda", "RDS", "S3" }),
                ("DevOps", "Full automation", new[] { "GitHub Actions", "Terraform", "Docker", "Helm" }),
            })
            .Select<(string B, string T, string[] P), (string, string, Action<JsonObject>)>(c =>
                ("ProductCard", c.T, child =>
                {
                    Text(child, "ProductCard", "Badge", c.B);
                    Text(child, "ProductCard", "Description", c.T);
                    Attach(child, "Points", Bag(c.P
                        .Select<string, (string, string, Action<JsonObject>)>(pt =>
                            ("ProductPoint", pt, _ => { }))
                        .ToArray()));
                }))
            .ToArray());

        var faqItems = ar
            ? new[]
            {
                ("هل تساعدون في الهجرة إلى السحابة؟", "نعم، نقيّم بنيتك الحالية ونخطط الهجرة تدريجياً مع تقليل التوقف إلى أدنى حد."),
                ("هل تقدّمون مراقبة بعد الإطلاق؟", "نعم، نセット لوحات مراقبة وتنبيهات ونبقى متاحين للاستجابة للحوادث."),
            }
            : new[]
            {
                ("Do you help with cloud migration?", "Yes, we assess your current infrastructure and plan a gradual migration with minimal downtime."),
                ("Do you provide monitoring after launch?", "Yes, we set up monitoring dashboards and alerts and remain available for incident response."),
            };

        var faqBag = Bag(faqItems
            .Select<(string Q, string A), (string, string, Action<JsonObject>)>(f =>
                ("FaqItem", f.Q, child =>
                    Field(child, "FaqItem", "Answer", new JsonObject { ["Html"] = $"<p>{f.A}</p>" })))
            .ToArray());

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "الحلول" : "Solutions");
                Text(child, "PageHeaderBlock", "Heading", ar
                    ? "السحابة و DevOps"
                    : "Cloud & DevOps");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "بنية سحابية تنشر نفسها، خطوط CI/CD، وحاويات تنمو مع عملك دون توقف."
                    : "Self-deploying cloud infrastructure, CI/CD pipelines, and containers that scale with your business.");
            }));

        var body = Section("body", "default", "container", "spacious",
            ("RichTextBlock", "Overview", child =>
            {
                Field(child, "RichTextBlock", "Body", new JsonObject
                {
                    ["Html"] = ar
                        ? "<p>نصمّم وننفّذ بنية سحابية متكاملة: من تخزين الكود إلى النشر التلقائي إلى المراقبة. نستخدم Azure و AWS حسب احتياجك.</p>"
                          + "<p>كل شيء ككود: البنية التحتية بـ Terraform، خطوط النشر بـ GitHub Actions، والحاويات بـ Docker و Kubernetes.</p>"
                        : "<p>We design and implement complete cloud infrastructure: from code storage to automated deployment to monitoring. We use Azure and AWS depending on your needs.</p>"
                          + "<p>Everything as code: infrastructure with Terraform, deployment pipelines with GitHub Actions, and containers with Docker and Kubernetes.</p>",
                });
            }));

        var featuresSection = Section("features", "alt", "container", "spacious",
            ("FeatureGridBlock", "Features", child =>
            {
                Text(child, "FeatureGridBlock", "Eyebrow", ar ? "المميزات" : "Features");
                Text(child, "FeatureGridBlock", "Heading", ar ? "ما الذي تحصل عليه" : "What you get");
                Attach(child, "Features", featuresBag);
            }));

        var techSection = Section("tech", "default", "container", "spacious",
            ("ProductGridBlock", "Tech", child =>
            {
                Text(child, "ProductGridBlock", "Eyebrow", ar ? "التقنيات" : "Technologies");
                Text(child, "ProductGridBlock", "Heading", ar ? "أدوات نستخدمها" : "Tools we use");
                Attach(child, "Products", techCards);
            }));

        var faqSection = Section("faq", "alt", "container", "spacious",
            ("FaqBlock", "FAQ", child =>
            {
                Text(child, "FaqBlock", "Heading", ar ? "أسئلة شائعة" : "FAQ");
                Attach(child, "Questions", faqBag);
            }));

        var cta = Section("cta", "dark", "container", "normal",
            ("CtaBlock", "CTA", child =>
            {
                Text(child, "CtaBlock", "Heading", ar
                    ? "جاهز تنتقل إلى السحابة؟"
                    : "Ready to move to the cloud?");
                Text(child, "CtaBlock", "Text", ar
                    ? "احكِ لنا عن بنيتك الحالية وسنقترح خطة هجرة آمنة."
                    : "Tell us about your current setup and we will propose a safe migration plan.");
                Text(child, "CtaBlock", "ButtonText", ar ? "ابدأ المحادثة" : "Start the conversation");
                Link(child, "CtaBlock", "ButtonLink", ar ? "/contact" : "/en/contact",
                     ar ? "ابدأ المحادثة" : "Start the conversation");
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Body", body),
            ("Section", "Features", featuresSection),
            ("Section", "Tech", techSection),
            ("Section", "FAQ", faqSection),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechsolvclu{suffix}".PadRight(26, '0')[..26], "Page",
            ar ? "السحابة و DevOps" : "Cloud & DevOps", item =>
            {
                Localize(item, culture, "trafotech-sol-cloud");
                Route(item, ar ? "solutions/cloud" : "en/solutions/cloud");
                Text(item, "Seo", "MetaTitle", ar
                    ? "السحابة و DevOps — ترافو تيك"
                    : "Cloud & DevOps — Travotech");
                Text(item, "Seo", "MetaDescription", ar
                    ? "بنية سحابية وخطوط CI/CD وحاويات على Azure و AWS و Docker و Kubernetes."
                    : "Cloud infrastructure, CI/CD pipelines and containers on Azure, AWS, Docker and Kubernetes.");
                Bool(item, "Seo", "NoIndex", false);
                Attach(item, "Sections", sections);
            });
    }

    // ================================================================
    // Fix header/footer home links: /Home does not match any route.
    // Arabic home -> "/", English home -> "/en".
    // ================================================================
    public async Task<int> UpdateFrom18Async()
    {
        foreach (var id in new[]
                 {
                     "trafotechheader00000000000", "trafotechheaderen000000000",
                     "trafotechfooter00000000000", "trafotechfooteren000000000",
                 })
        {
            var existing = await _contentManager.GetAsync(id, VersionOptions.Latest);
            if (existing is not null)
            {
                await _contentManager.RemoveAsync(existing);
            }
        }

        await SeedChromeSoftwareAsync("ar");
        await SeedChromeSoftwareAsync("en");

        foreach (var id in new[] { "trafotechheader00000000000", "trafotechheaderen000000000" })
        {
            await SetLogoAsync(id, "SiteHeader", "travotech-logo-new.png");
        }

        foreach (var id in new[] { "trafotechfooter00000000000", "trafotechfooteren000000000" })
        {
            await SetLogoAsync(id, "SiteFooter", "travotech-logo-light.png");
        }

        return 19;
    }

    // ---------------------------------------------------- chrome (software)

    private async Task SeedChromeSoftwareAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var navItems = ar
            ? new[] { ("الرئيسية", "/"), ("خدماتنا", "/services"),
                      ("عن الشركة", "/about"), ("تواصل معنا", "/contact") }
            : new[] { ("Home", "/en"), ("Services", "/en/services"),
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
                Text(item, "SiteHeader", "CtaText", ar ? "ابدأ مشروعك" : "Start your project");
                Link(item, "SiteHeader", "CtaLink", ar ? "/contact" : "/en/contact",
                     ar ? "ابدأ مشروعك" : "Start your project");
                Bool(item, "SiteHeader", "ShowLanguageSwitcher", true);
                Bool(item, "SiteHeader", "ShowThemeToggle", true);
                Bool(item, "SiteHeader", "Sticky", true);
                Attach(item, "NavLinks", navBag);
            });

        var columns = ar
            ? new[]
            {
                ("الشركة", new[] { ("الرئيسية", "/"), ("خدماتنا", "/services"),
                                   ("عن الشركة", "/about"), ("تواصل معنا", "/contact") }),
                ("خدماتنا", new[] { ("تطوير الويب", "/solutions/web"),
                                     ("تطبيقات الموبايل", "/solutions/mobile"),
                                     ("السحابة و DevOps", "/solutions/cloud"),
                                     ("تصميم واجهات المستخدم", "/services") }),
                ("تواصل", new[] { ("CreditPlus.Travo@gmail.com", "mailto:CreditPlus.Travo@gmail.com"),
                                  ("+962 6 000 0000", "tel:+96260000000") }),
            }
            : new[]
            {
                ("COMPANY", new[] { ("Home", "/en"), ("Services", "/en/services"),
                                    ("About", "/en/about"), ("Contact", "/en/contact") }),
                ("SERVICES", new[] { ("Web Development", "/en/solutions/web"),
                                     ("Mobile Apps", "/en/solutions/mobile"),
                                     ("Cloud & DevOps", "/en/solutions/cloud"),
                                     ("UI/UX Design", "/en/services") }),
                ("CONTACT", new[] { ("CreditPlus.Travo@gmail.com", "mailto:CreditPlus.Travo@gmail.com"),
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
                    ? "برمجيات تُحرّك أعمالك نحو الأمام"
                    : "Software that moves your business forward");
                Text(item, "SiteFooter", "Email", "CreditPlus.Travo@gmail.com");
                Text(item, "SiteFooter", "Phone", "+962 6 000 0000");
                Text(item, "SiteFooter", "Location", ar ? "عمّان، الأردن" : "Amman, Jordan");
                Text(item, "SiteFooter", "Copyright", ar
                    ? "© 2026 ترافو تيك. جميع الحقوق محفوظة."
                    : "© 2026 Travotech. All rights reserved.");
                Text(item, "SiteFooter", "ComplianceNote", ar
                    ? "نبني برمجيات موثوقة منذ 2019"
                    : "Building reliable software since 2019");
                Attach(item, "LinkColumns", columnsBag);
            });
    }

    // ------------------------------------------------------ home (software)

    private async Task SeedHomeSoftwareAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var stats = ar
            ? new[] { ("200", "+", "مشروع مُنجز"), ("40", "+", "مهندس ومصمّم"),
                      ("15", "+", "سوقاً حول العالم"), ("98", "%", "رضا العملاء") }
            : new[] { ("200", "+", "Projects delivered"), ("40", "+", "Engineers & designers"),
                      ("15", "+", "Markets served"), ("98", "%", "Client satisfaction") };

        var statsBag = Bag(stats
            .Select<(string V, string S, string L), (string, string, Action<JsonObject>)>(s =>
                ("StatItem", s.L, child =>
                {
                    Text(child, "StatItem", "Value", s.V);
                    Text(child, "StatItem", "Suffix", s.S);
                    Text(child, "StatItem", "Label", s.L);
                }))
            .ToArray());

        var hero = Section("hero", "dark", "full", "none",
            ("PageHeroBlock", "Hero", child =>
            {
                Text(child, "PageHeroBlock", "Eyebrow", ar ? "شركة تطوير برمجيات" : "Software development company");
                Text(child, "PageHeroBlock", "HeadingBefore", ar ? "نبني" : "We build");
                Text(child, "PageHeroBlock", "HeadingHighlight", ar ? "برمجيات" : "software");
                Text(child, "PageHeroBlock", "HeadingAfter", ar ? "تُحرّك أعمالك" : "that moves your business forward");
                Text(child, "PageHeroBlock", "Lead", ar
                    ? "من الفكرة إلى الإطلاق — منتجات ويب وموبايل وسحابة مبنية للتوسّع والسرعة والأمان."
                    : "From idea to launch — web, mobile and cloud products engineered to scale, perform and stay secure.");
                Text(child, "PageHeroBlock", "PrimaryText", ar ? "ابدأ مشروعك" : "Start your project");
                Link(child, "PageHeroBlock", "PrimaryLink", ar ? "/contact" : "/en/contact",
                     ar ? "ابدأ مشروعك" : "Start your project");
                Text(child, "PageHeroBlock", "SecondaryText", ar ? "استعرض خدماتنا" : "Explore services");
                Link(child, "PageHeroBlock", "SecondaryLink", ar ? "/services" : "/en/services",
                     ar ? "استعرض خدماتنا" : "Explore services");
                Text(child, "PageHeroBlock", "DialValue", "200+");
                Text(child, "PageHeroBlock", "DialLabel", ar ? "مشروع مُنجز" : "Projects delivered");
                Text(child, "PageHeroBlock", "BadgeTitle", "Agile");
                Text(child, "PageHeroBlock", "BadgeText", ar ? "تسليم رشيق كل أسبوعين" : "Agile delivery, every sprint");
                Attach(child, "Stats", statsBag);
            }));

        var features = ar
            ? new[]
            {
                ("bolt", "تسليم سريع ومنتظم", "سبرنتات كل أسبوعين مع نسخة عاملة في نهاية كل واحدة — ترى التقدّم لا الوعود."),
                ("link", "تكاملات نظيفة", "نربط أنظمتك وأدواتك الحالية عبر واجهات برمجية موثّقة ومختبرة."),
                ("growth", "مبني للتوسّع", "بنية تتحمّل عشرة مستخدمين أو عشرة ملايين دون إعادة بناء."),
                ("support", "نبقى بعد الإطلاق", "مراقبة وصيانة واتفاقية دعم تُبقي المنتج يعمل على مدار الساعة."),
            }
            : new[]
            {
                ("bolt", "Fast, predictable delivery", "Two-week sprints ending in a working build — you see progress, not promises."),
                ("link", "Clean integrations", "We connect to the tools and systems you already run through documented, tested APIs."),
                ("growth", "Built to scale", "Architecture that handles ten users or ten million without a rewrite."),
                ("support", "We stay after launch", "Monitoring, maintenance and a support agreement that keeps the product running."),
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
                    ? "شريك هندسي، لا مورّد فحسب"
                    : "An engineering partner, not just a vendor");
                Text(child, "FeatureGridBlock", "Lead", ar
                    ? "نتعامل مع منتجك كأنه منتجنا: قرارات واضحة، وكود نظيف، وتسليم في موعده."
                    : "We treat your product like our own: clear decisions, clean code, delivered on time.");
                Attach(child, "Features", featuresBag);
            }));

        var productCards = Bag((ar
            ? new[]
            {
                ("الويب", "تطبيقات الويب", "تطبيقات ولوحات تحكم سريعة وسهلة الوصول مبنية بأحدث الأطر.", "/solutions/web", new[] { "Angular و React و .NET", "لوحات تحكم وبوابات عملاء", "متجاوبة وسهلة الوصول" }),
                ("الموبايل", "تطبيقات الموبايل", "تطبيقات iOS وأندرويد بجودة أصلية من قاعدة كود واحدة قابلة للصيانة.", "/solutions/mobile", new[] { "iOS وأندرويد من كود واحد", "دعم دون اتصال", "منشورة على المتاجر" }),
                ("السحابة", "السحابة و DevOps", "خطوط نشر مؤتمتة وحاويات وبنية سحابية تنشر نفسها.", "/solutions/cloud", new[] { "CI/CD مؤتمت", "بنية تحتية ككود", "مراقبة وتنبيهات" }),
            }
            : new[]
            {
                ("Web", "Web Applications", "Fast, accessible web apps and dashboards built with modern frameworks.", "/en/solutions/web", new[] { "Angular, React & .NET", "Dashboards & customer portals", "Accessible & responsive" }),
                ("Mobile", "Mobile Apps", "Native-quality iOS and Android apps from a single, maintainable codebase.", "/en/solutions/mobile", new[] { "iOS & Android from one codebase", "Offline-first support", "App Store & Play published" }),
                ("Cloud", "Cloud & DevOps", "Automated pipelines, containers and cloud infrastructure that deploys itself.", "/en/solutions/cloud", new[] { "Automated CI/CD", "Infrastructure as code", "Monitoring & alerts" }),
            })
            .Select<(string B, string T, string D, string L, string[] P), (string, string, Action<JsonObject>)>(c =>
                ("ProductCard", c.T, child =>
                {
                    Text(child, "ProductCard", "Badge", c.B);
                    Text(child, "ProductCard", "Description", c.D);
                    Link(child, "ProductCard", "Link", c.L, ar ? "اعرف المزيد" : "Learn more");
                    Attach(child, "Points", Bag(c.P
                        .Select<string, (string, string, Action<JsonObject>)>(pt =>
                            ("ProductPoint", pt, _ => { }))
                        .ToArray()));
                }))
            .ToArray());

        var services = Section("services", "alt", "container", "spacious",
            ("ProductGridBlock", "Services", child =>
            {
                Text(child, "ProductGridBlock", "Eyebrow", ar ? "خدماتنا" : "Our services");
                Text(child, "ProductGridBlock", "Heading", ar
                    ? "كل ما تحتاجه لبناء منتج رقمي"
                    : "Everything you need to build a digital product");
                Text(child, "ProductGridBlock", "LinkText", ar ? "كل الخدمات" : "All services");
                Link(child, "ProductGridBlock", "Link", ar ? "/services" : "/en/services",
                     ar ? "كل الخدمات" : "All services");
                Attach(child, "Products", productCards);
            }));

        // --- Process section -----------------------------------------------
        var processSteps = ar
            ? new[]
            {
                ("search", "اكتشاف وتحليل", "نبدأ بفهم تحديك وأهدافك ونقاط مستخدميك، ونخرج بخارطة طريق واضحة."),
                ("pen", "التصميم والنماذج الأولية", "نحوّل المتطلبات إلى نماذج أولية قابلة للنقر قبل كتابة أي كود."),
                ("code", "التطوير والبناء", "سبرنتات كل أسبوعين مع نسخة عاملة، ترى التقدّم وتجرّب الميزات."),
                ("rocket", "الإطلاق والدعم", "ننشر، نراقب، ونبقى معك بعد الإطلاق بصيانة ودعم مستمر."),
            }
            : new[]
            {
                ("search", "Discover & Analyse", "We start by understanding your challenge, goals and users, and produce a clear roadmap."),
                ("pen", "Design & Prototype", "We turn requirements into clickable prototypes before writing any code."),
                ("code", "Build & Develop", "Two-week sprints with a working build — you see progress and test features."),
                ("rocket", "Launch & Support", "We deploy, monitor and stay with you after launch with ongoing maintenance."),
            };

        var processBag = Bag(processSteps
            .Select<(string I, string T, string D), (string, string, Action<JsonObject>)>(s =>
                ("FeatureItem", s.T, child =>
                {
                    Text(child, "FeatureItem", "Icon", s.I);
                    Text(child, "FeatureItem", "Description", s.D);
                }))
            .ToArray());

        var process = Section("process", "default", "container", "spacious",
            ("FeatureGridBlock", "Process", child =>
            {
                Text(child, "FeatureGridBlock", "Eyebrow", ar ? "كيف نعمل" : "How we work");
                Text(child, "FeatureGridBlock", "Heading", ar
                    ? "أربع خطوات من الفكرة إلى الإطلاق"
                    : "Four steps from idea to launch");
                Text(child, "FeatureGridBlock", "Lead", ar
                    ? "منهجية واضحة وشفافة تبقيك في الصورة في كل مرحلة."
                    : "A clear, transparent methodology that keeps you in the picture at every stage.");
                Attach(child, "Features", processBag);
            }));

        // --- Industries section --------------------------------------------
        var industries = ar
            ? new[]
            {
                ("bank", "الخدمات المالية", "أنظمة دفع، محافظ إلكترونية وبوابات تقرير."),
                ("cart", "التجارة الإلكترونية", "متاجر ومنصّات طلبات بتجربة شراء سلسة."),
                ("heart", "الرعاية الصحية", "سجلات المرضى والمواعيد والتطبيب عن بعد."),
                ("factory", "التصنيع واللوجستيات", "تتبّع المخزون وإدارة العمليات ولوحات الأداء."),
                ("graduation", "التعليم", "منصّات تعليم وإدارة فصول واختبارات."),
                ("building", "العقارات", "إدارة المحافظ والعقود والعملاء."),
            }
            : new[]
            {
                ("bank", "Financial Services", "Payment systems, e-wallets and reporting portals."),
                ("cart", "E-Commerce", "Stores and order platforms with a seamless checkout."),
                ("heart", "Healthcare", "Patient records, appointments and telemedicine."),
                ("factory", "Manufacturing & Logistics", "Inventory tracking, operations management and dashboards."),
                ("graduation", "Education", "Learning platforms, class management and assessments."),
                ("building", "Real Estate", "Portfolio, contract and client management."),
            };

        var industriesBag = Bag(industries
            .Select<(string I, string T, string D), (string, string, Action<JsonObject>)>(ind =>
                ("ProductCard", ind.T, child =>
                {
                    Text(child, "ProductCard", "Badge", ind.I);
                    Text(child, "ProductCard", "Description", ind.D);
                }))
            .ToArray());

        var industriesSection = Section("industries", "alt", "container", "spacious",
            ("ProductGridBlock", "Industries", child =>
            {
                Text(child, "ProductGridBlock", "Eyebrow", ar ? "القطاعات" : "Industries");
                Text(child, "ProductGridBlock", "Heading", ar
                    ? "خبرة في قطاعات متعددة"
                    : "Expertise across industries");
                Attach(child, "Products", industriesBag);
            }));

        // --- Tech stack section --------------------------------------------
        var techStack = ar
            ? new[]
            {
                ("code", "الواجهات الأمامية", "Angular, React, Vue, TypeScript, TailwindCSS"),
                ("server", "الخوادم والـ APIs", ".NET, Node.js, Python, REST, GraphQL"),
                ("database", "قواعد البيانات", "PostgreSQL, SQL Server, MongoDB, Redis"),
                ("cloud", "السحابة و DevOps", "Azure, AWS, Docker, Kubernetes, GitHub Actions"),
            }
            : new[]
            {
                ("code", "Frontend", "Angular, React, Vue, TypeScript, TailwindCSS"),
                ("server", "Backend & APIs", ".NET, Node.js, Python, REST, GraphQL"),
                ("database", "Databases", "PostgreSQL, SQL Server, MongoDB, Redis"),
                ("cloud", "Cloud & DevOps", "Azure, AWS, Docker, Kubernetes, GitHub Actions"),
            };

        var techBag = Bag(techStack
            .Select<(string I, string T, string D), (string, string, Action<JsonObject>)>(t =>
                ("FeatureItem", t.T, child =>
                {
                    Text(child, "FeatureItem", "Icon", t.I);
                    Text(child, "FeatureItem", "Description", t.D);
                }))
            .ToArray());

        var techSection = Section("tech", "default", "container", "spacious",
            ("FeatureGridBlock", "TechStack", child =>
            {
                Text(child, "FeatureGridBlock", "Eyebrow", ar ? "التقنيات" : "Tech stack");
                Text(child, "FeatureGridBlock", "Heading", ar
                    ? "أدوات نثق بها ونعرفها جيداً"
                    : "Tools we know and trust");
                Text(child, "FeatureGridBlock", "Lead", ar
                    ? "نختار التقنية حسب الحاجة لا حسب الموضة — كل ما سبق مُختبر في الإنتاج."
                    : "We pick technology by need, not fashion — all of the above is battle-tested in production.");
                Attach(child, "Features", techBag);
            }));

        // --- FAQ section ---------------------------------------------------
        var faqItems = ar
            ? new[]
            {
                ("كم يستغرق بناء مشروع من الصفر؟", "يعتمد على النطاق، لكن معظم مشاريعنا تنتقل من الفكرة إلى نسخة أولية عاملة في ٤-٨ أسابيع، وإلى إطلاق كامل في ٣-٦ أشهر."),
                ("هل تعملون بعقود ثابتة أم بالساعة؟", "كلاهما متاح. للمشاريع ذات النطاق الواضح نقدّم سعراً ثابتاً، وللمشاريع المرنة نعمل بالساعة مع تقارير أسبوعية."),
                ("هل أبقى أملك الكود المصدري؟", "نعم، كودك ملكك بالكامل من اليوم الأول. نسلّمه على مستودع Git خاص بك مع توثيق كامل."),
                ("هل تقدّمون الدعم بعد الإطلاق؟", "نعم، نقدّم اتفاقيات دعم وصيانة تتراوح من المراقبة الأساسية إلى الدعم على مدار الساعة حسب احتياجك."),
            }
            : new[]
            {
                ("How long does it take to build a project from scratch?", "It depends on scope, but most of our projects go from idea to a working prototype in 4-8 weeks, and to full launch in 3-6 months."),
                ("Do you work on fixed-price or hourly contracts?", "Both are available. For well-scoped projects we offer a fixed price; for flexible projects we work hourly with weekly reports."),
                ("Do I own the source code?", "Yes, your code is entirely yours from day one. We deliver it on your own Git repository with full documentation."),
                ("Do you provide post-launch support?", "Yes, we offer support and maintenance agreements ranging from basic monitoring to 24/7 support depending on your needs."),
            };

        var faqBag = Bag(faqItems
            .Select<(string Q, string A), (string, string, Action<JsonObject>)>(f =>
                ("FaqItem", f.Q, child =>
                    Field(child, "FaqItem", "Answer", new JsonObject { ["Html"] = $"<p>{f.A}</p>" })))
            .ToArray());

        var faqSection = Section("faq", "alt", "container", "spacious",
            ("FaqBlock", "FAQ", child =>
            {
                Text(child, "FaqBlock", "Heading", ar
                    ? "أسئلة شائعة"
                    : "Frequently asked questions");
                Attach(child, "Questions", faqBag);
            }));

        var cta = Section("cta", "dark", "container", "normal",
            ("CtaBlock", "CTA", child =>
            {
                Text(child, "CtaBlock", "Heading", ar
                    ? "عندك فكرة مشروع؟"
                    : "Have a project in mind?");
                Text(child, "CtaBlock", "Text", ar
                    ? "احكِ لنا عن فكرتك وسنعود إليك بخطة وتقدير خلال يوم عمل واحد."
                    : "Tell us about your idea and we will come back with a plan and an estimate within one business day.");
                Text(child, "CtaBlock", "ButtonText", ar ? "ابدأ المحادثة" : "Start the conversation");
                Link(child, "CtaBlock", "ButtonLink", ar ? "/contact" : "/en/contact",
                     ar ? "ابدأ المحادثة" : "Start the conversation");
            }));

        var sections = Bag(
            ("Section", "Hero", hero),
            ("Section", "Why", why),
            ("Section", "Services", services),
            ("Section", "Process", process),
            ("Section", "Industries", industriesSection),
            ("Section", "TechStack", techSection),
            ("Section", "FAQ", faqSection),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechhome{suffix}".PadRight(26, '0')[..26], "Page", "Home", item =>
        {
            Localize(item, culture, "trafotech-home");
            Route(item, ar ? "home" : "en", homepage: ar);
            Text(item, "Seo", "MetaTitle", ar
                ? "ترافو تيك — شركة تطوير برمجيات"
                : "Travotech — Software development company");
            Text(item, "Seo", "MetaDescription", ar
                ? "نبني تطبيقات الويب والموبايل والأنظمة السحابية المخصّصة للشركات."
                : "We build custom web, mobile and cloud software for businesses.");
            Bool(item, "Seo", "NoIndex", false);
            Attach(item, "Sections", sections);
        });
    }

    // -------------------------------------------------- services (software)

    private async Task SeedServicesSoftwareAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var categories = ar
            ? new[] { ("web", "الويب"), ("mobile", "الموبايل"),
                      ("cloud", "السحابة و DevOps"), ("design", "التصميم والاستشارات") }
            : new[] { ("web", "Web"), ("mobile", "Mobile"),
                      ("cloud", "Cloud & DevOps"), ("design", "Design & Consulting") };

        var categoriesBag = Bag(categories
            .Select<(string K, string L), (string, string, Action<JsonObject>)>(c =>
                ("ProductCategory", c.L, child => Text(child, "ProductCategory", "Key", c.K)))
            .ToArray());

        var services = ar
            ? new[]
            {
                ("hub", "web", "تطوير تطبيقات الويب", "تطبيقات ويب متقدّمة ولوحات تحكم وبوابات عملاء مبنية بـ Angular وReact و.NET.", "/solutions/web", new[] { "واجهات حديثة قائمة على المكوّنات", "واجهات برمجية آمنة وموثّقة", "متجاوبة وسهلة الوصول افتراضياً" }),
                ("card", "web", "برمجيات مخصّصة للمؤسسات", "أنظمة أعمال مصمّمة على طريقة عمل فريقك، تستبدل جداول البيانات والخطوات اليدوية.", "", new[] { "مصمّمة حسب عملياتك", "صلاحيات حسب الدور وسجل تدقيق", "تتكامل مع أنظمتك الحالية" }),
                ("bolt", "mobile", "تطوير تطبيقات الموبايل", "تطبيقات iOS وأندرويد من قاعدة كود واحدة، مع دعم دون اتصال وإشعارات فورية.", "/solutions/mobile", new[] { "قاعدة كود واحدة للمنصّتين", "تعمل دون اتصال عند الحاجة", "منشورة على App Store وGoogle Play" }),
                ("growth", "cloud", "السحابة و DevOps", "نشر بالحاويات وخطوط CI/CD وبنية تحتية ككود على Azure وAWS.", "/solutions/cloud", new[] { "بناء ونشر مؤتمت", "بنية تحتية ككود", "مراقبة وتنبيهات مدمجة" }),
                ("check", "cloud", "اختبار الجودة والأتمتة", "اختبار آلي ويدوي يكتشف المشاكل قبل أن يصل إليها مستخدموك.", "", new[] { "مجموعات اختبار انحدار آلية", "اختبار الأداء والحِمل", "تقارير أخطاء واضحة قابلة للتكرار" }),
                ("design", "design", "تصميم تجربة وواجهة المستخدم", "بحث ونماذج أولية وواجهات مصقولة تجعل المنتجات المعقّدة تبدو بسيطة.", "", new[] { "بحث المستخدم والنماذج الأولية", "أنظمة تصميم لا شاشات منفردة", "جاهزة للتسليم للتطوير" }),
                ("link", "web", "الواجهات البرمجية والتكامل", "ربط أنظمتك ببوابات الدفع وأنظمة ERP والخدمات الخارجية.", "", new[] { "واجهات REST وGraphQL", "تكامل مع أطراف ثالثة", "تحديث الأنظمة القديمة" }),
            }
            : new[]
            {
                ("hub", "web", "Web Application Development", "Progressive web apps, dashboards and customer portals built with Angular, React and .NET.", "/en/solutions/web", new[] { "Modern, component-based front ends", "Secure, well-documented APIs", "Accessible and responsive by default" }),
                ("card", "web", "Custom & Enterprise Software", "Line-of-business systems tailored to how your team actually works, replacing spreadsheets and manual steps.", "", new[] { "Tailored to your processes", "Role-based access and audit trails", "Integrates with your existing stack" }),
                ("bolt", "mobile", "Mobile App Development", "iOS and Android apps from a single codebase, with offline support and push notifications.", "/en/solutions/mobile", new[] { "One codebase, both platforms", "Offline-first where it matters", "Published to the App Store and Google Play" }),
                ("growth", "cloud", "Cloud & DevOps", "Containerised deployments, CI/CD pipelines and infrastructure as code on Azure and AWS.", "/en/solutions/cloud", new[] { "Automated build and deploy", "Infrastructure as code", "Monitoring and alerting built in" }),
                ("check", "cloud", "QA & Test Automation", "Automated and manual testing that catches problems before your users do.", "", new[] { "Automated regression suites", "Performance and load testing", "Clear, reproducible bug reports" }),
                ("design", "design", "UI/UX Design", "Research, wireframes and polished interfaces that make complex products feel simple.", "", new[] { "User research and prototyping", "Design systems, not one-off screens", "Handoff-ready for engineering" }),
                ("link", "web", "API & Integrations", "Connecting your systems to payment gateways, ERPs and third-party services.", "", new[] { "REST and GraphQL APIs", "Third-party integrations", "Legacy system modernisation" }),
            };

        var rowEntries = new List<(string, string, Action<JsonObject>)>();
        foreach (var (icon, cat, title, desc, linkUrl, points) in services)
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
                if (!string.IsNullOrEmpty(linkUrl))
                {
                    Link(child, "ProductRow", "Link", linkUrl, ar ? "اعرف المزيد" : "Learn more");
                }
                Attach(child, "Points", pointsBag);
            }));
        }

        var rowsBag = Bag(rowEntries.ToArray());

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "خدماتنا" : "Services");
                Text(child, "PageHeaderBlock", "Heading", ar
                    ? "خدمات تغطّي دورة حياة منتجك بالكامل"
                    : "Services that cover your product's whole lifecycle");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "من أول نموذج أولي إلى التشغيل والصيانة في الإنتاج — نبقى معك في كل مرحلة."
                    : "From the first prototype to running and maintaining it in production — we are with you at every stage.");
            }));

        var list = Section("list", "alt", "container", "spacious",
            ("ProductListBlock", "Services", child =>
            {
                Text(child, "ProductListBlock", "AllLabel", ar ? "الكل" : "All");
                Bool(child, "ProductListBlock", "ShowFilters", true);
                Attach(child, "Categories", categoriesBag);
                Attach(child, "Rows", rowsBag);
            }));

        // --- FAQ section ---------------------------------------------------
        var svcFaqItems = ar
            ? new[]
            {
                ("ما الفرق بين تطبيق الويب وتطبيق الموبايل؟", "تطبيق الويب يعمل في المتصفح ولا يحتاج تثبيت، بينما تطبيق الموبايل يُثبّت على الجهاز ويعمل دون اتصال. نساعدك على اختيار الأنسب لجمهورك."),
                ("هل تستخدمون تقنيات مفتوحة المصدر؟", "نعم، نستخدم مزيجاً من التقنيات مفتوحة المصدر والتجارية حسب متطلبات المشروع وميزانيته."),
                ("هل يمكنكم العمل مع فريقنا الداخلي؟", "بالتأكيد. نندمج مع فريقك ونستخدم نفس أدوات إدارة المشاريع والتواصل لضمان سير العمل بسلاسة."),
            }
            : new[]
            {
                ("What is the difference between a web app and a mobile app?", "A web app runs in the browser and needs no installation, while a mobile app is installed on the device and can work offline. We help you choose what fits your audience."),
                ("Do you use open-source technologies?", "Yes, we use a mix of open-source and commercial technologies depending on project requirements and budget."),
                ("Can you work with our in-house team?", "Absolutely. We integrate with your team and use the same project management and communication tools to ensure smooth collaboration."),
            };

        var svcFaqBag = Bag(svcFaqItems
            .Select<(string Q, string A), (string, string, Action<JsonObject>)>(f =>
                ("FaqItem", f.Q, child =>
                    Field(child, "FaqItem", "Answer", new JsonObject { ["Html"] = $"<p>{f.A}</p>" })))
            .ToArray());

        var svcFaqSection = Section("faq", "default", "container", "spacious",
            ("FaqBlock", "FAQ", child =>
            {
                Text(child, "FaqBlock", "Heading", ar ? "أسئلة شائعة عن خدماتنا" : "FAQ about our services");
                Attach(child, "Questions", svcFaqBag);
            }));

        var cta = Section("cta", "default", "container", "spacious",
            ("CtaBlock", "CTA", child =>
            {
                Text(child, "CtaBlock", "Heading", ar ? "غير متأكد من أين تبدأ؟" : "Not sure where to start?");
                Text(child, "CtaBlock", "Text", ar
                    ? "احكِ لنا عن التحدي الذي تواجهه وسنقترح عليك الطريق الأنسب بصراحة."
                    : "Tell us about the challenge you are facing and we will honestly suggest the best way forward.");
                Text(child, "CtaBlock", "ButtonText", ar ? "تواصل معنا" : "Contact us");
                Link(child, "CtaBlock", "ButtonLink", ar ? "/contact" : "/en/contact",
                     ar ? "تواصل معنا" : "Contact us");
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Services", list),
            ("Section", "FAQ", svcFaqSection),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechproducts{suffix}".PadRight(26, '0')[..26], "Page",
            "Services", item =>
            {
                Localize(item, culture, "trafotech-products");
                Route(item, ar ? "services" : "en/services");
                Text(item, "Seo", "MetaTitle", ar ? "خدماتنا — ترافو تيك" : "Services — Travotech");
                Text(item, "Seo", "MetaDescription", ar
                    ? "خدمات تطوير الويب والموبايل والسحابة والتصميم من ترافو تيك."
                    : "Web, mobile, cloud and design development services from Travotech.");
                Bool(item, "Seo", "NoIndex", false);
                Attach(item, "Sections", sections);
            });
    }

    // ----------------------------------------------------- about (software)

    private async Task SeedAboutSoftwareAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var stats = ar
            ? new[] { ("200", "+", "مشروع مُنجز"), ("40", "+", "مهندس ومصمّم"),
                      ("15", "+", "سوقاً حول العالم"), ("98", "%", "رضا العملاء") }
            : new[] { ("200", "+", "Projects delivered"), ("40", "+", "Engineers & designers"),
                      ("15", "+", "Markets served"), ("98", "%", "Client satisfaction") };

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
                ("2019", "الانطلاق", "بدأنا بفريق صغير من المهندسين وهدف واحد: بناء برمجيات يعتمد عليها الناس كل يوم."),
                ("2021", "أول عملاء المؤسسات", "سلّمنا أول أنظمة أعمال متكاملة، وبدأت الشراكات طويلة الأمد."),
                ("2023", "الموبايل والسحابة", "أضفنا فرق الموبايل و DevOps، وخدمنا عملاء في عدّة أسواق."),
                ("2025", "فريق من أربعين+", "نمونا إلى أكثر من أربعين مهندساً ومصمّماً، مع الحفاظ على معايير الجودة نفسها."),
            }
            : new[]
            {
                ("2019", "The start", "We began with a small team of engineers and one goal: build software people rely on every day."),
                ("2021", "First enterprise clients", "Delivered our first end-to-end business systems and began long-term partnerships."),
                ("2023", "Mobile and cloud", "Added mobile and DevOps teams and served clients across several markets."),
                ("2025", "A team of 40+", "Grew to more than forty engineers and designers, holding the same quality bar."),
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
                ("جودة نفخر بها", "نكتب كوداً نظيفاً ونختبره، لأن ما نسلّمه اليوم سيصونه أحدهم بعد سنوات."),
                ("شفافية كاملة", "تعرف أين وصل مشروعك في أي لحظة — تقدير صادق ومواعيد واقعية بلا مفاجآت."),
                ("شراكة لا صفقة", "لا نسلّم المشروع ونمضي؛ نبقى شركاء في نجاح المنتج على المدى الطويل."),
            }
            : new[]
            {
                ("Quality we're proud of", "We write clean, tested code, because what we ship today someone maintains years from now."),
                ("Full transparency", "You know where your project stands at any moment — honest estimates and realistic dates, no surprises."),
                ("A partnership, not a deal", "We do not hand over a project and leave; we stay invested in the product's long-term success."),
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
                    ? "نحن نبني البرمجيات، وأنت تبني أعمالك"
                    : "We build the software, you build the business");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "منذ 2019 ونحن نساعد الشركات على تحويل أفكارها إلى منتجات رقمية يعتمد عليها مستخدموها."
                    : "Since 2019 we have helped companies turn ideas into digital products their users rely on.");
            }));

        var story = Section("story", "default", "container", "spacious",
            ("StoryBlock", "Story", child =>
            {
                Text(child, "StoryBlock", "Eyebrow", ar ? "قصتنا" : "Our story");
                Text(child, "StoryBlock", "Heading", ar
                    ? "بدأنا من إيمان بسيط"
                    : "We started from a simple belief");
                Field(child, "StoryBlock", "Body", new JsonObject
                {
                    ["Html"] = ar
                        ? "<p>رأينا شركات كثيرة عالقة بين أنظمة قديمة وجداول بيانات لا تنتهي، وأفكار عظيمة تنتظر من يبنيها. فقرّرنا أن نكون ذلك الفريق: مهندسون يفهمون العمل قبل الكود.</p>"
                          + "<p>اليوم نبني تطبيقات الويب والموبايل والأنظمة السحابية لعملاء في عدّة أسواق، والمقياس لم يتغيّر: هل يعتمد المستخدم على ما بنيناه كل يوم؟</p>"
                        : "<p>We kept seeing companies stuck between legacy systems and endless spreadsheets, with great ideas waiting for someone to build them. We decided to be that team: engineers who understand the business before the code.</p>"
                          + "<p>Today we build web, mobile and cloud systems for clients across several markets, and the measure has not changed: do people rely on what we built, every day?</p>",
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

        // --- Capabilities section -----------------------------------------
        var capabilities = ar
            ? new[]
            {
                ("code", "هندسة الواجهات", "Angular, React, Vue, TypeScript — مكوّنات قابلة لإعادة الاستخدام وأداء عالٍ."),
                ("server", "هندسة الخوادم", ".NET و Node.js و Python — واجهات REST و GraphQL آمنة وسريعة."),
                ("database", "تصميم البيانات", "نمذجة قواعد البيانات العلاقية وغير العلاقية مع هجرات آمنة."),
                ("cloud", "البنية السحابية", "Azure و AWS و Docker و Kubernetes — نشر مؤتمت ومراقبة."),
                ("shield", "الأمن والموثوقية", "اختبار اختراق، مراجعة كود أمنية، وامتثال GDPR و PCI."),
                ("users", "تجربة المستخدم", "بحث، نماذج أولية، اختبار قابلية استخدام، وأنظمة تصميم."),
            }
            : new[]
            {
                ("code", "Frontend Engineering", "Angular, React, Vue, TypeScript — reusable components and high performance."),
                ("server", "Backend Engineering", ".NET, Node.js, Python — secure, fast REST and GraphQL APIs."),
                ("database", "Data Design", "Relational and non-relational database modelling with safe migrations."),
                ("cloud", "Cloud Infrastructure", "Azure, AWS, Docker, Kubernetes — automated deployment and monitoring."),
                ("shield", "Security & Reliability", "Penetration testing, secure code review, GDPR and PCI compliance."),
                ("users", "User Experience", "Research, prototyping, usability testing, and design systems."),
            };

        var capabilitiesBag = Bag(capabilities
            .Select<(string I, string T, string D), (string, string, Action<JsonObject>)>(c =>
                ("FeatureItem", c.T, child =>
                {
                    Text(child, "FeatureItem", "Icon", c.I);
                    Text(child, "FeatureItem", "Description", c.D);
                }))
            .ToArray());

        var capabilitiesSection = Section("capabilities", "alt", "container", "spacious",
            ("FeatureGridBlock", "Capabilities", child =>
            {
                Text(child, "FeatureGridBlock", "Eyebrow", ar ? "قدراتنا" : "Our capabilities");
                Text(child, "FeatureGridBlock", "Heading", ar
                    ? "مهارات تقنية تغطي المكدّس بالكامل"
                    : "Technical skills across the full stack");
                Text(child, "FeatureGridBlock", "Lead", ar
                    ? "من الواجهة إلى قاعدة البيانات إلى السحابة — نمتلك الخبرة في كل طبقة."
                    : "From frontend to database to cloud — we have expertise in every layer.");
                Attach(child, "Features", capabilitiesBag);
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
            ("Section", "Capabilities", capabilitiesSection),
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechabout{suffix}".PadRight(26, '0')[..26], "Page", "About", item =>
        {
            Localize(item, culture, "trafotech-about");
            Route(item, ar ? "about" : "en/about");
            Text(item, "Seo", "MetaTitle", ar ? "عن الشركة — ترافو تيك" : "About — Travotech");
            Text(item, "Seo", "MetaDescription", ar
                ? "شركة تطوير برمجيات تساعد الشركات على بناء منتجات رقمية موثوقة منذ 2019."
                : "A software company helping businesses build reliable digital products since 2019.");
            Bool(item, "Seo", "NoIndex", false);
            Attach(item, "Sections", sections);
        });
    }

    // --------------------------------------------------- contact (software)

    private async Task SeedContactSoftwareAsync(string culture)
    {
        var ar = culture == "ar";
        var suffix = ar ? string.Empty : "en";

        var header = Section("top", "dark", "container", "spacious",
            ("PageHeaderBlock", "Header", child =>
            {
                Text(child, "PageHeaderBlock", "Eyebrow", ar ? "تواصل معنا" : "Contact");
                Text(child, "PageHeaderBlock", "Heading", ar ? "لنبدأ الحديث" : "Let's start the conversation");
                Text(child, "PageHeaderBlock", "Lead", ar
                    ? "احكِ لنا عن مشروعك أو فكرتك. نقرأ كل رسالة ونعود خلال يوم عمل واحد."
                    : "Tell us about your project or idea. We read every message and reply within one business day.");
            }));

        var body = Section("form", "default", "container", "spacious",
            ("ContactFormBlock", "Form", child =>
            {
                Text(child, "ContactFormBlock", "Heading", ar ? "راسلنا" : "Send us a message");
                Text(child, "ContactFormBlock", "RecipientEmail", "CreditPlus.Travo@gmail.com");
                Text(child, "ContactFormBlock", "SubmitButtonText", ar ? "إرسال" : "Send");
                Text(child, "ContactFormBlock", "SuccessMessage", ar
                    ? "تم استلام رسالتك، شكراً لتواصلك معنا."
                    : "Thanks for reaching out — we have received your message.");
                Text(child, "ContactFormBlock", "NameLabel", ar ? "الاسم" : "Name");
                Text(child, "ContactFormBlock", "EmailLabel", ar ? "البريد الإلكتروني" : "Email");
                Text(child, "ContactFormBlock", "OrganisationLabel", ar ? "الجهة (اختياري)" : "Organisation (optional)");
                Text(child, "ContactFormBlock", "MessageLabel", ar ? "رسالتك" : "Your message");
                Bool(child, "ContactFormBlock", "ShowOrganisation", true);
            }),
            ("ContactInfoBlock", "Details", child =>
            {
                Text(child, "ContactInfoBlock", "Heading", ar ? "معلومات التواصل" : "Contact details");
                Text(child, "ContactInfoBlock", "Email", "CreditPlus.Travo@gmail.com");
                Text(child, "ContactInfoBlock", "Phone", "+962 6 000 0000");
                Text(child, "ContactInfoBlock", "Address", ar ? "عمّان، الأردن" : "Amman, Jordan");
                Text(child, "ContactInfoBlock", "Hours", ar
                    ? "الأحد إلى الخميس، ٩ صباحاً – ٦ مساءً"
                    : "Sunday to Thursday, 9 AM – 6 PM");
                Text(child, "ContactInfoBlock", "Note", ar
                    ? "للعملاء الحاليين: استخدم قناة الدعم في اتفاقية الخدمة للطلبات العاجلة."
                    : "Existing clients: use the support channel in your service agreement for urgent requests.");
                Text(child, "ContactInfoBlock", "EmailLabel", ar ? "البريد" : "Email");
                Text(child, "ContactInfoBlock", "PhoneLabel", ar ? "الهاتف" : "Phone");
                Text(child, "ContactInfoBlock", "AddressLabel", ar ? "العنوان" : "Address");
                Text(child, "ContactInfoBlock", "HoursLabel", ar ? "ساعات العمل" : "Working hours");
            }));

        // --- FAQ section ---------------------------------------------------
        var contactFaqItems = ar
            ? new[]
            {
                ("كم تستغرق الرد على رسالتي؟", "نعود إليك خلال يوم عمل واحد (الأحد إلى الخميس). للطلبات العاجلة من العملاء الحاليين، استخدم قناة الدعم في اتفاقية الخدمة."),
                ("ما المعلومات التي يجب أن أرسلها؟", "صف فكرتك أو تحديك باختصار، أخبرنا عن حجم فريقك والجدول الزمني المتوقع، وأي ميزانية تقريبية. كلما أعطيتنا تفاصيل أكثر، كان ردنا أدق."),
                ("هل توقّعون اتفاقية عدم إفشاء (NDA)؟", "نعم، نوقّع NDA قبل أي نقاش تقني تفصيلي لحماية فكرتك ومعلوماتك التجارية."),
            }
            : new[]
            {
                ("How long until you reply to my message?", "We reply within one business day (Sunday to Thursday). For urgent requests from existing clients, use the support channel in your service agreement."),
                ("What information should I include?", "Describe your idea or challenge briefly, tell us about your team size and expected timeline, and any approximate budget. The more detail you give, the more accurate our response."),
                ("Do you sign an NDA?", "Yes, we sign an NDA before any detailed technical discussion to protect your idea and business information."),
            };

        var contactFaqBag = Bag(contactFaqItems
            .Select<(string Q, string A), (string, string, Action<JsonObject>)>(f =>
                ("FaqItem", f.Q, child =>
                    Field(child, "FaqItem", "Answer", new JsonObject { ["Html"] = $"<p>{f.A}</p>" })))
            .ToArray());

        var contactFaqSection = Section("faq", "alt", "container", "spacious",
            ("FaqBlock", "FAQ", child =>
            {
                Text(child, "FaqBlock", "Heading", ar ? "أسئلة شائعة" : "Frequently asked questions");
                Attach(child, "Questions", contactFaqBag);
            }));

        var sections = Bag(
            ("Section", "Header", header),
            ("Section", "Contact", body),
            ("Section", "FAQ", contactFaqSection));

        await EnsureAsync($"trafotechcontact{suffix}".PadRight(26, '0')[..26], "Page",
            "Contact", item =>
            {
                Localize(item, culture, "trafotech-contact");
                Route(item, ar ? "contact" : "en/contact");
                Text(item, "Seo", "MetaTitle", ar ? "تواصل معنا — ترافو تيك" : "Contact — Travotech");
                Text(item, "Seo", "MetaDescription", ar
                    ? "تواصل مع فريق ترافو تيك لبناء منتجك الرقمي القادم."
                    : "Get in touch with the Travotech team to build your next digital product.");
                Bool(item, "Seo", "NoIndex", false);
                Attach(item, "Sections", sections);
            });
    }
}
