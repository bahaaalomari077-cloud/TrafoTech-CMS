using OrchardCore.Autoroute.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.Flows.Models;
using OrchardCore.Media.Settings;
using OrchardCore.Title.Models;

namespace TrafoTech.Admin.Migrations;

/// <summary>
/// Defines every content type the site uses.
///
/// These run against whatever database is already there, on every start, and
/// Orchard records the version reached per migration class. That is the point:
/// adding a type means appending an UpdateFromN method, not rebuilding the
/// database — so editor content survives schema changes.
/// </summary>
public sealed class ContentTypeMigrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public ContentTypeMigrations(IContentDefinitionManager contentDefinitionManager)
    {
        _contentDefinitionManager = contentDefinitionManager;
    }

    public async Task<int> CreateAsync()
    {
        await DefineFieldPartsAsync();
        await DefineBlockTypesAsync();
        await DefineSectionAsync();
        await DefinePageTypesAsync();
        await DefineChromeAsync();

        return 1;
    }

    /// <summary>
    /// Switches the rich-text fields to the WYSIWYG editor.
    ///
    /// HtmlField defaults to a code editor, so authors were shown raw markup
    /// (&lt;h2&gt;…&lt;/h2&gt;) instead of formatted text. Wysiwyg gives them the usual
    /// toolbar — headings, bold, lists, links — and stores the same HTML.
    /// </summary>
    public async Task<int> UpdateFrom1Async()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync("RichTextBlock", part => part
            .WithField("Body", f => f
                .OfType("HtmlField").WithDisplayName("Body").WithPosition("0")
                .WithEditor("Wysiwyg")));

        await _contentDefinitionManager.AlterPartDefinitionAsync("FaqItem", part => part
            .WithField("Answer", f => f
                .OfType("HtmlField").WithDisplayName("Answer").WithPosition("0")
                .WithEditor("Wysiwyg")));

        return 2;
    }

    // ---------------------------------------------------------------- parts

    private async Task DefineFieldPartsAsync()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync("Seo", part => part
            .Attachable()
            .WithDescription("Search engine and social sharing metadata.")
            .WithField("MetaTitle", f => f
                .OfType("TextField").WithDisplayName("Meta title").WithPosition("0"))
            .WithField("MetaDescription", f => f
                .OfType("TextField").WithDisplayName("Meta description").WithPosition("1")
                .WithEditor("TextArea"))
            .WithField("MetaKeywords", f => f
                .OfType("TextField").WithDisplayName("Meta keywords").WithPosition("2"))
            .WithField("CanonicalUrl", f => f
                .OfType("TextField").WithDisplayName("Canonical URL").WithPosition("3"))
            .WithField("OpenGraphImage", f => f
                .OfType("MediaField").WithDisplayName("Social share image").WithPosition("4")
                .WithSettings(new MediaFieldSettings { Multiple = false }))
            .WithField("NoIndex", f => f
                .OfType("BooleanField").WithDisplayName("Hide from search engines").WithPosition("5")));

        await _contentDefinitionManager.AlterPartDefinitionAsync("Section", part => part
            .Attachable()
            .WithField("Anchor", f => f
                .OfType("TextField").WithDisplayName("Anchor id").WithPosition("0"))
            .WithField("Background", f => f
                .OfType("TextField").WithDisplayName("Background").WithPosition("1")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList("default", "Default", "alt", "Alternate",
                                             "brand", "Brand", "dark", "Dark")))
            .WithField("Width", f => f
                .OfType("TextField").WithDisplayName("Width").WithPosition("2")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList("container", "Container", "wide", "Wide",
                                             "full", "Full bleed")))
            .WithField("Spacing", f => f
                .OfType("TextField").WithDisplayName("Vertical spacing").WithPosition("3")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList("normal", "Normal", "compact", "Compact",
                                             "spacious", "Spacious", "none", "None")))
            .WithField("Hidden", f => f
                .OfType("BooleanField").WithDisplayName("Hide this section").WithPosition("4")));
    }

    private static TextFieldPredefinedListEditorSettings PredefinedList(params string[] pairs)
    {
        var options = new List<ListValueOption>();
        for (var i = 0; i < pairs.Length; i += 2)
        {
            options.Add(new ListValueOption { Value = pairs[i], Name = pairs[i + 1] });
        }

        return new TextFieldPredefinedListEditorSettings
        {
            Editor = EditorOption.Dropdown,
            DefaultValue = pairs.Length > 0 ? pairs[0] : string.Empty,
            Options = options.ToArray(),
        };
    }

    // --------------------------------------------------------------- blocks

    private async Task DefineBlockTypesAsync()
    {
        // Repeatable leaf items first: bags below reference them by name.
        await SimpleWidgetAsync("StatItem", "Statistic", part => part
            .WithField("Value", f => f.OfType("TextField").WithDisplayName("Value").WithPosition("0"))
            .WithField("Suffix", f => f.OfType("TextField").WithDisplayName("Suffix").WithPosition("1"))
            .WithField("Label", f => f.OfType("TextField").WithDisplayName("Label").WithPosition("2")));

        await SimpleWidgetAsync("FeatureItem", "Feature", part => part
            .WithField("Icon", f => f
                .OfType("TextField").WithDisplayName("Icon").WithPosition("0")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList("shield", "Shield", "growth", "Growth",
                                             "link", "Link", "support", "Support",
                                             "bolt", "Bolt", "clock", "Clock")))
            .WithField("Description", f => f
                .OfType("TextField").WithDisplayName("Description").WithPosition("1")
                .WithEditor("TextArea")));

        await SimpleWidgetAsync("ProductPoint", "Point", null);

        await SimpleWidgetAsync("ProductCategory", "Product category", part => part
            .WithField("Key", f => f
                .OfType("TextField").WithDisplayName("Key (matches a product's category)")
                .WithPosition("0")));

        await SimpleWidgetAsync("NavLink", "Navigation link", part => part
            .WithField("Link", f => f.OfType("LinkField").WithDisplayName("Link").WithPosition("0")));

        await SimpleWidgetAsync("FooterLink", "Footer link", part => part
            .WithField("Link", f => f.OfType("LinkField").WithDisplayName("Link").WithPosition("0")));

        await SimpleWidgetAsync("SocialLink", "Social link", part => part
            .WithField("Network", f => f
                .OfType("TextField").WithDisplayName("Network").WithPosition("0")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList("facebook", "Facebook", "instagram", "Instagram",
                                             "linkedin", "LinkedIn", "x", "X",
                                             "youtube", "YouTube", "whatsapp", "WhatsApp")))
            .WithField("Url", f => f.OfType("TextField").WithDisplayName("URL").WithPosition("1")));

        // Card with its own bag of points.
        await SimpleWidgetAsync("ProductCard", "Product card", part => part
            .WithField("Badge", f => f.OfType("TextField").WithDisplayName("Category badge").WithPosition("0"))
            .WithField("Description", f => f
                .OfType("TextField").WithDisplayName("Description").WithPosition("1").WithEditor("TextArea"))
            .WithField("Link", f => f.OfType("LinkField").WithDisplayName("Link").WithPosition("2")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("ProductCard", type => type
            .WithPart("Points", "BagPart", p => p
                .WithDisplayName("Points")
                .WithPosition("Parts:2#Points;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["ProductPoint"],
                    DisplayType = "Detail",
                })));

        await SimpleWidgetAsync("ProductRow", "Product", part => part
            .WithField("Icon", f => f
                .OfType("TextField").WithDisplayName("Icon").WithPosition("0")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList("activity", "Activity", "bolt", "Bolt",
                                             "wallet", "Wallet", "check", "Check",
                                             "hub", "Hub", "card", "Card")))
            .WithField("Category", f => f.OfType("TextField").WithDisplayName("Category key").WithPosition("1"))
            .WithField("CategoryLabel", f => f.OfType("TextField").WithDisplayName("Category label").WithPosition("2"))
            .WithField("Description", f => f
                .OfType("TextField").WithDisplayName("Description").WithPosition("3").WithEditor("TextArea"))
            .WithField("Link", f => f.OfType("LinkField").WithDisplayName("Link").WithPosition("4")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("ProductRow", type => type
            .WithPart("Points", "BagPart", p => p
                .WithDisplayName("Points")
                .WithPosition("Parts:2#Points;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["ProductPoint"],
                    DisplayType = "Detail",
                })));

        await SimpleWidgetAsync("FaqItem", "Question", part => part
            .WithField("Answer", f => f
                .OfType("HtmlField").WithDisplayName("Answer").WithPosition("0")
                .WithEditor("Wysiwyg")));

        // --- Composite blocks ---

        await SimpleWidgetAsync("PageHeroBlock", "Page hero", part => part
            .WithField("Eyebrow", f => f.OfType("TextField").WithDisplayName("Eyebrow").WithPosition("0"))
            .WithField("HeadingBefore", f => f.OfType("TextField").WithDisplayName("Heading (before highlight)").WithPosition("1"))
            .WithField("HeadingHighlight", f => f.OfType("TextField").WithDisplayName("Highlighted word").WithPosition("2"))
            .WithField("HeadingAfter", f => f.OfType("TextField").WithDisplayName("Heading (after highlight)").WithPosition("3"))
            .WithField("Lead", f => f.OfType("TextField").WithDisplayName("Lead paragraph").WithPosition("4").WithEditor("TextArea"))
            .WithField("PrimaryText", f => f.OfType("TextField").WithDisplayName("Primary button text").WithPosition("5"))
            .WithField("PrimaryLink", f => f.OfType("LinkField").WithDisplayName("Primary button link").WithPosition("6"))
            .WithField("SecondaryText", f => f.OfType("TextField").WithDisplayName("Secondary button text").WithPosition("7"))
            .WithField("SecondaryLink", f => f.OfType("LinkField").WithDisplayName("Secondary button link").WithPosition("8"))
            .WithField("DialValue", f => f.OfType("TextField").WithDisplayName("Dial value").WithPosition("9"))
            .WithField("DialLabel", f => f.OfType("TextField").WithDisplayName("Dial label").WithPosition("10"))
            .WithField("BadgeTitle", f => f.OfType("TextField").WithDisplayName("Floating badge title").WithPosition("11"))
            .WithField("BadgeText", f => f.OfType("TextField").WithDisplayName("Floating badge text").WithPosition("12")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("PageHeroBlock", type => type
            .WithPart("Stats", "BagPart", p => p
                .WithDisplayName("Stats")
                .WithPosition("Parts:2#Stats;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["StatItem"],
                    DisplayType = "Detail",
                })));

        await SimpleWidgetAsync("PageHeaderBlock", "Page header", part => part
            .WithField("Eyebrow", f => f.OfType("TextField").WithDisplayName("Eyebrow").WithPosition("0"))
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("1"))
            .WithField("Lead", f => f.OfType("TextField").WithDisplayName("Lead paragraph").WithPosition("2").WithEditor("TextArea")));

        await SimpleWidgetAsync("RichTextBlock", "Rich text", part => part
            .WithField("Body", f => f
                .OfType("HtmlField").WithDisplayName("Body").WithPosition("0")
                .WithEditor("Wysiwyg")));

        await SimpleWidgetAsync("MediaBlock", "Image / Video", part => part
            .WithField("Media", f => f
                .OfType("MediaField").WithDisplayName("Media").WithPosition("0")
                .WithSettings(new MediaFieldSettings { Multiple = false }))
            .WithField("Caption", f => f.OfType("TextField").WithDisplayName("Caption").WithPosition("1")));

        await SimpleWidgetAsync("FeatureGridBlock", "Feature grid", part => part
            .WithField("Eyebrow", f => f.OfType("TextField").WithDisplayName("Eyebrow").WithPosition("0"))
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("1"))
            .WithField("Lead", f => f.OfType("TextField").WithDisplayName("Lead paragraph").WithPosition("2").WithEditor("TextArea")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("FeatureGridBlock", type => type
            .WithPart("Features", "BagPart", p => p
                .WithDisplayName("Features")
                .WithPosition("Parts:2#Features;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["FeatureItem"],
                    DisplayType = "Detail",
                })));

        await SimpleWidgetAsync("ProductGridBlock", "Product grid", part => part
            .WithField("Eyebrow", f => f.OfType("TextField").WithDisplayName("Eyebrow").WithPosition("0"))
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("1"))
            .WithField("LinkText", f => f.OfType("TextField").WithDisplayName("Link text").WithPosition("2"))
            .WithField("Link", f => f.OfType("LinkField").WithDisplayName("Link").WithPosition("3")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("ProductGridBlock", type => type
            .WithPart("Products", "BagPart", p => p
                .WithDisplayName("Products")
                .WithPosition("Parts:2#Products;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["ProductCard"],
                    DisplayType = "Detail",
                })));

        await SimpleWidgetAsync("ProductListBlock", "Product list", part => part
            .WithField("AllLabel", f => f.OfType("TextField").WithDisplayName("\"All\" filter label").WithPosition("0"))
            .WithField("ShowFilters", f => f.OfType("BooleanField").WithDisplayName("Show category filters").WithPosition("1")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("ProductListBlock", type => type
            .WithPart("Categories", "BagPart", p => p
                .WithDisplayName("Categories")
                .WithPosition("Parts:2#Categories;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["ProductCategory"],
                    DisplayType = "Detail",
                }))
            .WithPart("Rows", "BagPart", p => p
                .WithDisplayName("Products")
                .WithPosition("Parts:3#Products;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["ProductRow"],
                    DisplayType = "Detail",
                })));

        await SimpleWidgetAsync("FaqBlock", "FAQ", part => part
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("0")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("FaqBlock", type => type
            .WithPart("Questions", "BagPart", p => p
                .WithDisplayName("Questions")
                .WithPosition("Parts:2#Questions;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["FaqItem"],
                    DisplayType = "Detail",
                })));

        await SimpleWidgetAsync("CtaBlock", "Call to action", part => part
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("0"))
            .WithField("Text", f => f.OfType("TextField").WithDisplayName("Text").WithPosition("1").WithEditor("TextArea"))
            .WithField("ButtonText", f => f.OfType("TextField").WithDisplayName("Button text").WithPosition("2"))
            .WithField("ButtonLink", f => f.OfType("LinkField").WithDisplayName("Button link").WithPosition("3")));

        await SimpleWidgetAsync("ContactFormBlock", "Contact form", part => part
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("0"))
            .WithField("Intro", f => f.OfType("TextField").WithDisplayName("Intro").WithPosition("1").WithEditor("TextArea"))
            .WithField("RecipientEmail", f => f.OfType("TextField").WithDisplayName("Recipient email").WithPosition("2"))
            .WithField("SubmitButtonText", f => f.OfType("TextField").WithDisplayName("Submit button text").WithPosition("3"))
            .WithField("SuccessMessage", f => f.OfType("TextField").WithDisplayName("Success message").WithPosition("4").WithEditor("TextArea")));

        await SimpleWidgetAsync("FooterLinkColumn", "Footer link column", null);

        await _contentDefinitionManager.AlterTypeDefinitionAsync("FooterLinkColumn", type => type
            .WithPart("Links", "BagPart", p => p
                .WithDisplayName("Links")
                .WithPosition("Parts:1#Links;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["FooterLink"],
                    DisplayType = "Detail",
                })));
    }

    /// <summary>
    /// A widget type with a TitlePart and its own field part. Creatable matters:
    /// a type that is not creatable never registers in the GraphQL schema, and
    /// bags containing it fail to resolve.
    /// </summary>
    private async Task SimpleWidgetAsync(
        string name,
        string displayName,
        Action<OrchardCore.ContentManagement.Metadata.Builders.ContentPartDefinitionBuilder>? fields)
    {
        if (fields is not null)
        {
            await _contentDefinitionManager.AlterPartDefinitionAsync(name, part =>
            {
                part.Attachable();
                fields(part);
            });
        }

        await _contentDefinitionManager.AlterTypeDefinitionAsync(name, type =>
        {
            type.DisplayedAs(displayName)
                .Creatable()
                .Draftable()
                .Versionable()
                .Stereotype("Widget")
                .WithPart(nameof(TitlePart), p => p.WithPosition("0"));

            if (fields is not null)
            {
                type.WithPart(name, p => p.WithPosition("1"));
            }
        });
    }

    // -------------------------------------------------------------- section

    private async Task DefineSectionAsync()
    {
        await _contentDefinitionManager.AlterTypeDefinitionAsync("Section", type => type
            .DisplayedAs("Section")
            .Draftable()
            .Versionable()
            .Stereotype("Widget")
            .WithPart(nameof(TitlePart), p => p.WithPosition("0"))
            .WithPart("Section", p => p.WithPosition("1"))
            .WithPart("Blocks", "BagPart", p => p
                .WithDisplayName("Blocks")
                .WithPosition("Parts:2#Blocks;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes =
                    [
                        "PageHeroBlock", "PageHeaderBlock", "RichTextBlock", "MediaBlock",
                        "FeatureGridBlock", "ProductGridBlock", "ProductListBlock",
                        "FaqBlock", "CtaBlock", "ContactFormBlock",
                    ],
                    DisplayType = "Detail",
                })));
    }

    // ---------------------------------------------------------------- pages

    private async Task DefinePageTypesAsync()
    {
        await _contentDefinitionManager.AlterTypeDefinitionAsync("Page", type => type
            .DisplayedAs("Page")
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .Securable()
            .WithPart(nameof(TitlePart), p => p.WithPosition("Content;0"))
            .WithPart("AutoroutePart", p => p
                .WithPosition("Content;1")
                .WithSettings(new AutoroutePartSettings
                {
                    Pattern = "{{ ContentItem.DisplayText | slugify }}",
                    AllowCustomPath = true,
                    ShowHomepageOption = true,
                }))
            .WithPart("LocalizationPart", p => p.WithPosition("Content;2"))
            .WithPart("Seo", p => p.WithDisplayName("SEO").WithPosition("Parts:5#SEO;0"))
            .WithPart("Sections", "BagPart", p => p
                .WithDisplayName("Sections")
                .WithPosition("Parts:10#Sections;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["Section"],
                    DisplayType = "Detail",
                })));
    }

    // --------------------------------------------------------------- chrome

    private async Task DefineChromeAsync()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync("SiteHeader", part => part
            .Attachable()
            .WithField("BrandName", f => f.OfType("TextField").WithDisplayName("Brand name").WithPosition("0"))
            .WithField("Logo", f => f
                .OfType("MediaField").WithDisplayName("Logo").WithPosition("1")
                .WithSettings(new MediaFieldSettings { Multiple = false }))
            .WithField("CtaText", f => f.OfType("TextField").WithDisplayName("CTA button text").WithPosition("2"))
            .WithField("CtaLink", f => f.OfType("LinkField").WithDisplayName("CTA button link").WithPosition("3"))
            .WithField("ShowLanguageSwitcher", f => f.OfType("BooleanField").WithDisplayName("Show language switcher").WithPosition("4"))
            .WithField("ShowThemeToggle", f => f.OfType("BooleanField").WithDisplayName("Show theme toggle").WithPosition("5"))
            .WithField("Sticky", f => f.OfType("BooleanField").WithDisplayName("Sticky header").WithPosition("6")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("SiteHeader", type => type
            .DisplayedAs("Site header")
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .Securable()
            .WithPart(nameof(TitlePart), p => p.WithPosition("Content;0"))
            .WithPart("LocalizationPart", p => p.WithPosition("Content;1"))
            .WithPart("SiteHeader", p => p.WithPosition("Content;2"))
            .WithPart("NavLinks", "BagPart", p => p
                .WithDisplayName("Navigation")
                .WithPosition("Parts:5#Navigation;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["NavLink"],
                    DisplayType = "Detail",
                }))
            .WithPart("SocialLinks", "BagPart", p => p
                .WithDisplayName("Social links")
                .WithPosition("Parts:6#Social;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["SocialLink"],
                    DisplayType = "Detail",
                })));

        await _contentDefinitionManager.AlterPartDefinitionAsync("SiteFooter", part => part
            .Attachable()
            .WithField("BrandName", f => f.OfType("TextField").WithDisplayName("Brand name").WithPosition("0"))
            .WithField("Logo", f => f
                .OfType("MediaField").WithDisplayName("Logo").WithPosition("1")
                .WithSettings(new MediaFieldSettings { Multiple = false }))
            .WithField("Tagline", f => f.OfType("TextField").WithDisplayName("Tagline").WithPosition("2").WithEditor("TextArea"))
            .WithField("Email", f => f.OfType("TextField").WithDisplayName("Email").WithPosition("3"))
            .WithField("Phone", f => f.OfType("TextField").WithDisplayName("Phone").WithPosition("4"))
            .WithField("Location", f => f.OfType("TextField").WithDisplayName("Location").WithPosition("5"))
            .WithField("Copyright", f => f.OfType("TextField").WithDisplayName("Copyright text").WithPosition("6"))
            .WithField("ComplianceNote", f => f.OfType("TextField").WithDisplayName("Compliance note").WithPosition("7")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("SiteFooter", type => type
            .DisplayedAs("Site footer")
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .Securable()
            .WithPart(nameof(TitlePart), p => p.WithPosition("Content;0"))
            .WithPart("LocalizationPart", p => p.WithPosition("Content;1"))
            .WithPart("SiteFooter", p => p.WithPosition("Content;2"))
            .WithPart("LinkColumns", "BagPart", p => p
                .WithDisplayName("Link columns")
                .WithPosition("Parts:5#Links;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["FooterLinkColumn"],
                    DisplayType = "Detail",
                }))
            .WithPart("SocialLinks", "BagPart", p => p
                .WithDisplayName("Social links")
                .WithPosition("Parts:6#Social;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["SocialLink"],
                    DisplayType = "Detail",
                })));

        await _contentDefinitionManager.AlterPartDefinitionAsync("Address", part => part
            .Attachable()
            .WithField("Label", f => f.OfType("TextField").WithDisplayName("Label").WithPosition("0"))
            .WithField("Street", f => f.OfType("TextField").WithDisplayName("Street").WithPosition("1"))
            .WithField("City", f => f.OfType("TextField").WithDisplayName("City").WithPosition("2"))
            .WithField("Country", f => f.OfType("TextField").WithDisplayName("Country").WithPosition("3"))
            .WithField("Phone", f => f.OfType("TextField").WithDisplayName("Phone").WithPosition("4"))
            .WithField("Email", f => f.OfType("TextField").WithDisplayName("Email").WithPosition("5"))
            .WithField("Latitude", f => f.OfType("TextField").WithDisplayName("Latitude").WithPosition("6"))
            .WithField("Longitude", f => f.OfType("TextField").WithDisplayName("Longitude").WithPosition("7"))
            .WithField("WorkingHours", f => f.OfType("TextField").WithDisplayName("Working hours").WithPosition("8").WithEditor("TextArea")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("Address", type => type
            .DisplayedAs("Address")
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .Securable()
            .WithPart(nameof(TitlePart), p => p.WithPosition("0"))
            .WithPart("LocalizationPart", p => p.WithPosition("1"))
            .WithPart("Address", p => p.WithPosition("2")));
    }
}
