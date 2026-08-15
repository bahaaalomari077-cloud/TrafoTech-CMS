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
            Text(item, "Seo", "MetaTitle", ar ? "عن الشركة — ترافو تيك" : "About — Travotek");
            Text(item, "Seo", "MetaDescription", ar
                ? "تعرّف على فريق ترافو تيك."
                : "Meet the Travotek team.");
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
                Text(item, "SiteHeader", "BrandName", ar ? "ترافو تيك" : "Travotek");
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
                ("تواصل", new[] { ("hello@travotek.com", "mailto:hello@travotek.com"),
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
                ("CONTACT", new[] { ("hello@travotek.com", "mailto:hello@travotek.com"),
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
                Text(item, "SiteFooter", "BrandName", ar ? "ترافو تيك" : "Travotek");
                Text(item, "SiteFooter", "Tagline", ar
                    ? "بنية الدفع الفوري للاقتصاد الرقمي"
                    : "Real-time payment infrastructure for the digital economy");
                Text(item, "SiteFooter", "Email", "hello@travotek.com");
                Text(item, "SiteFooter", "Phone", "+962 6 000 0000");
                Text(item, "SiteFooter", "Location", ar ? "عمّان، الأردن" : "Amman, Jordan");
                Text(item, "SiteFooter", "Copyright", ar
                    ? "© 2026 ترافو تيك. جميع الحقوق محفوظة."
                    : "© 2026 Travotek. All rights reserved.");
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
                Text(child, "PageHeroBlock", "Eyebrow", ar ? "مرحباً بكم في ترافو تيك" : "Welcome to Travotek");
                Text(child, "PageHeroBlock", "HeadingBefore", ar ? "حلول" : "Travotek");
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
                Text(child, "FeatureGridBlock", "Eyebrow", ar ? "لماذا ترافو تيك" : "Why Travotek");
                Text(child, "FeatureGridBlock", "Heading", ar
                    ? "بنية تحتية يُبنى عليها اقتصاد"
                    : "Infrastructure an economy can be built on");
                Text(child, "FeatureGridBlock", "Lead", ar
                    ? "كل قرار هندسي في منصتنا يعود إلى سؤال واحد: ماذا يحدث لو توقف هذا النظام لدقيقة واحدة؟"
                    : "Every engineering decision comes back to one question: what happens if this system stops for a single minute?");
                Attach(child, "Features", featuresBag);
            }));

        var cta = Section("cta", "dark", "full", "normal",
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
            ("Section", "CTA", cta));

        await EnsureAsync($"trafotechhome{suffix}".PadRight(26, '0')[..26], "Page", "Home", item =>
        {
            Localize(item, culture, "trafotech-home");
            Route(item, ar ? "home" : "en", homepage: ar);
            Text(item, "Seo", "MetaTitle", ar
                ? "ترافو تيك — بنية الدفع الفوري للاقتصاد الرقمي"
                : "Travotek — Instant payment infrastructure for the digital economy");
            Text(item, "Seo", "MetaDescription", ar
                ? "حلول ترافو تيك لتمويل سلاسل التوريد وبنية الدفع الفوري."
                : "Travotek solutions for supply chain finance and instant payment infrastructure.");
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
                Text(item, "Seo", "MetaTitle", ar ? "المنتجات — ترافو تيك" : "Products — Travotek");
                Text(item, "Seo", "MetaDescription", ar
                    ? "منظومة دفع متكاملة من ترافو تيك."
                    : "A complete payments ecosystem from Travotek.");
                Bool(item, "Seo", "NoIndex", false);
                Attach(item, "Sections", sections);
            });
    }
}
