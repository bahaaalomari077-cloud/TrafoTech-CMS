using OrchardCore.Autoroute.Models;
using OrchardCore.ContentManagement.GraphQL.Settings;
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

    /// <summary>
    /// Adds the blocks the About page needs: a story with a stat panel, a
    /// milestone timeline, and numbered value cards.
    /// </summary>
    public async Task<int> UpdateFrom2Async()
    {
        await SimpleWidgetAsync("MilestoneItem", "Milestone", part => part
            .WithField("Year", f => f.OfType("TextField").WithDisplayName("Year").WithPosition("0"))
            .WithField("Description", f => f
                .OfType("TextField").WithDisplayName("Description").WithPosition("1")
                .WithEditor("TextArea")));

        await SimpleWidgetAsync("ValueItem", "Value", part => part
            .WithField("Description", f => f
                .OfType("TextField").WithDisplayName("Description").WithPosition("0")
                .WithEditor("TextArea")));

        await SimpleWidgetAsync("StoryBlock", "Story with stats", part => part
            .WithField("Eyebrow", f => f.OfType("TextField").WithDisplayName("Eyebrow").WithPosition("0"))
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("1"))
            .WithField("Body", f => f
                .OfType("HtmlField").WithDisplayName("Body").WithPosition("2")
                .WithEditor("Wysiwyg")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("StoryBlock", type => type
            .WithPart("Stats", "BagPart", p => p
                .WithDisplayName("Stats")
                .WithPosition("Parts:2#Stats;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["StatItem"],
                    DisplayType = "Detail",
                })));

        await SimpleWidgetAsync("TimelineBlock", "Timeline", part => part
            .WithField("Eyebrow", f => f.OfType("TextField").WithDisplayName("Eyebrow").WithPosition("0"))
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("1")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("TimelineBlock", type => type
            .WithPart("Milestones", "BagPart", p => p
                .WithDisplayName("Milestones")
                .WithPosition("Parts:2#Milestones;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["MilestoneItem"],
                    DisplayType = "Detail",
                })));

        await SimpleWidgetAsync("ValuesBlock", "Values", part => part
            .WithField("Eyebrow", f => f.OfType("TextField").WithDisplayName("Eyebrow").WithPosition("0"))
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("1")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("ValuesBlock", type => type
            .WithPart("Values", "BagPart", p => p
                .WithDisplayName("Values")
                .WithPosition("Parts:2#Values;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["ValueItem"],
                    DisplayType = "Detail",
                })));

        // Contact details shown beside the form.
        await SimpleWidgetAsync("ContactInfoBlock", "Contact details", part => part
            .WithField("Heading", f => f.OfType("TextField").WithDisplayName("Heading").WithPosition("0"))
            .WithField("Email", f => f.OfType("TextField").WithDisplayName("Email").WithPosition("1"))
            .WithField("Phone", f => f.OfType("TextField").WithDisplayName("Phone").WithPosition("2"))
            .WithField("Note", f => f
                .OfType("TextField").WithDisplayName("Note").WithPosition("3")
                .WithEditor("TextArea")));

        // The contact form gains an optional organisation field.
        await _contentDefinitionManager.AlterPartDefinitionAsync("ContactFormBlock", part => part
            .WithField("ShowOrganisation", f => f
                .OfType("BooleanField").WithDisplayName("Show organisation field").WithPosition("5")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("Section", type => type
            .WithPart("Blocks", "BagPart", p => p
                .WithDisplayName("Blocks")
                .WithPosition("Parts:2#Blocks;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes =
                    [
                        "PageHeroBlock", "PageHeaderBlock", "RichTextBlock", "MediaBlock",
                        "FeatureGridBlock", "ProductGridBlock", "ProductListBlock",
                        "StoryBlock", "TimelineBlock", "ValuesBlock",
                        "FaqBlock", "CtaBlock", "ContactFormBlock", "ContactInfoBlock",
                    ],
                    DisplayType = "Detail",
                })));

        return 3;
    }

    /// <summary>
    /// Adds the address and working-hours rows to the contact details card.
    /// </summary>
    public async Task<int> UpdateFrom3Async()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync("ContactInfoBlock", part => part
            .WithField("Address", f => f
                .OfType("TextField").WithDisplayName("Address").WithPosition("3"))
            .WithField("Hours", f => f
                .OfType("TextField").WithDisplayName("Working hours").WithPosition("4")
                .WithEditor("TextArea")));

        return 4;
    }

    /// <summary>
    /// Adds the newsletter types: Contact, ContactGroup and Campaign.
    ///
    /// These are admin-only by construction. None of them carries an
    /// AutoroutePart, so they have no URL and cannot be reached from the site;
    /// the frontend also never queries them. Keeping them route-less is what
    /// makes that guarantee structural rather than a matter of configuration.
    /// </summary>
    public async Task<int> UpdateFrom4Async()
    {
        // ---- Contact: one person -------------------------------------------
        await _contentDefinitionManager.AlterPartDefinitionAsync("Contact", part => part
            .WithField("Email", f => f
                .OfType("TextField").WithDisplayName("Email").WithPosition("0")
                .WithSettings(new TextFieldSettings
                {
                    Required = true,
                    Hint = "The address this person receives campaigns at.",
                }))
            .WithField("Organisation", f => f
                .OfType("TextField").WithDisplayName("Organisation").WithPosition("1"))
            .WithField("Subscribed", f => f
                .OfType("BooleanField").WithDisplayName("Subscribed").WithPosition("2")
                .WithSettings(new BooleanFieldSettings
                {
                    DefaultValue = true,
                    Hint = "Clear this to stop sending to the person without deleting them.",
                })));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("Contact", type => type
            .DisplayedAs("Contact")
            .Creatable()
            .Listable()
            .Securable()
            .WithPart(nameof(TitlePart), p => p
                .WithDisplayName("Name")
                .WithPosition("0"))
            .WithPart("Contact", p => p.WithPosition("1")));

        // ---- ContactGroup: a named set of contacts -------------------------
        await _contentDefinitionManager.AlterPartDefinitionAsync("ContactGroup", part => part
            .WithField("Members", f => f
                .OfType("ContentPickerField").WithDisplayName("Members").WithPosition("0")
                .WithSettings(new ContentPickerFieldSettings
                {
                    Multiple = true,
                    DisplayedContentTypes = ["Contact"],
                    Hint = "The people in this group.",
                })));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("ContactGroup", type => type
            .DisplayedAs("Contact group")
            .Creatable()
            .Listable()
            .Securable()
            .WithPart(nameof(TitlePart), p => p
                .WithDisplayName("Group name")
                .WithPosition("0"))
            .WithPart("ContactGroup", p => p.WithPosition("1")));

        // ---- Campaign: the message plus who it goes to ---------------------
        await _contentDefinitionManager.AlterPartDefinitionAsync("Campaign", part => part
            .WithField("Subject", f => f
                .OfType("TextField").WithDisplayName("Subject").WithPosition("0")
                .WithSettings(new TextFieldSettings
                {
                    Required = true,
                    Hint = "The email subject line.",
                }))
            .WithField("Body", f => f
                .OfType("HtmlField").WithDisplayName("Message").WithPosition("1")
                .WithEditor("Wysiwyg"))
            .WithField("Attachment", f => f
                .OfType("MediaField").WithDisplayName("Image or video").WithPosition("2")
                .WithSettings(new MediaFieldSettings
                {
                    Multiple = false,
                    Hint = "Shown in the email as a link to the uploaded file.",
                }))
            .WithField("Recipients", f => f
                .OfType("ContentPickerField").WithDisplayName("Send to people").WithPosition("3")
                .WithSettings(new ContentPickerFieldSettings
                {
                    Multiple = true,
                    DisplayedContentTypes = ["Contact"],
                    Hint = "Individual recipients. Combine with groups if you like.",
                }))
            .WithField("RecipientGroups", f => f
                .OfType("ContentPickerField").WithDisplayName("Send to groups").WithPosition("4")
                .WithSettings(new ContentPickerFieldSettings
                {
                    Multiple = true,
                    DisplayedContentTypes = ["ContactGroup"],
                    Hint = "Everyone in the chosen groups receives this.",
                })));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("Campaign", type => type
            .DisplayedAs("Campaign")
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .Securable()
            .WithPart(nameof(TitlePart), p => p
                .WithDisplayName("Internal name")
                .WithPosition("0"))
            .WithPart("Campaign", p => p.WithPosition("1")));

        return 5;
    }

    /// <summary>
    /// Hides the newsletter types from the GraphQL schema.
    ///
    /// Orchard exposes every content type through the API by default, and the
    /// endpoint is readable anonymously — so without this a visitor could query
    /// the contact list and read every stored address. Being route-less keeps
    /// these types off the site; this keeps them out of the API as well.
    /// </summary>
    public async Task<int> UpdateFrom5Async()
    {
        foreach (var name in new[] { "Contact", "ContactGroup", "Campaign" })
        {
            await _contentDefinitionManager.AlterTypeDefinitionAsync(name, type => type
                .WithSettings(new GraphQLContentTypeSettings { Hidden = true }));
        }

        return 6;
    }

    /// <summary>
    /// Takes the newsletter types out of the shared Content Items list.
    ///
    /// They are a separate tool, not site content, and listing them beside the
    /// pages mixes two unrelated jobs. Clearing Listable removes them from that
    /// list and its type filter; the Newsletter menu still reaches them, since
    /// a type-filtered list works regardless of the flag.
    /// </summary>
    public async Task<int> UpdateFrom6Async()
    {
        foreach (var name in new[] { "Contact", "ContactGroup", "Campaign" })
        {
            await _contentDefinitionManager.AlterTypeDefinitionAsync(name, type => type
                .Listable(false));
        }

        return 7;
    }

    /// <summary>
    /// Adds the newsletter structure: a campaign can hold sections, and each
    /// section holds news items.
    ///
    /// This is what turns a campaign from a single message into a monthly
    /// newsletter — "Members news", "Sector news", "Announcements" — where each
    /// story carries its own image, date and link. Both types are widgets, so
    /// they are only ever created inside a campaign and never appear as
    /// standalone content.
    /// </summary>
    public async Task<int> UpdateFrom7Async()
    {
        await SimpleWidgetAsync("NewsItem", "News item", part => part
            .WithField("Summary", f => f
                .OfType("TextField").WithDisplayName("Summary").WithPosition("0")
                .WithEditor("TextArea")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Optional line under the headline.",
                }))
            .WithField("Date", f => f
                .OfType("TextField").WithDisplayName("Date").WithPosition("1")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Shown as written, e.g. Monday, July 27, 2026.",
                }))
            .WithField("Link", f => f
                .OfType("LinkField").WithDisplayName("Read more link").WithPosition("2"))
            .WithField("Image", f => f
                .OfType("MediaField").WithDisplayName("Thumbnail").WithPosition("3")
                .WithSettings(new MediaFieldSettings
                {
                    Multiple = false,
                    Hint = "Travels with the email, so keep it small — about 300px wide.",
                })));

        await SimpleWidgetAsync("NewsSection", "Newsletter section", part => part
            .WithField("Heading", f => f
                .OfType("TextField").WithDisplayName("Section heading").WithPosition("0")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "The blue bar, e.g. Members News.",
                })));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("NewsSection", type => type
            .WithPart("Items", "BagPart", p => p
                .WithDisplayName("News items")
                .WithPosition("Parts:2#Items;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["NewsItem"],
                    DisplayType = "Detail",
                })));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("Campaign", type => type
            .WithPart("Sections", "BagPart", p => p
                .WithDisplayName("Newsletter sections")
                .WithPosition("Parts:5#Sections;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["NewsSection"],
                    DisplayType = "Detail",
                })));

        // Header and footer text for the generated newsletter.
        await _contentDefinitionManager.AlterPartDefinitionAsync("Campaign", part => part
            .WithField("Edition", f => f
                .OfType("TextField").WithDisplayName("Edition").WithPosition("5")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Printed at the top, e.g. JULY 2026.",
                }))
            .WithField("FooterNote", f => f
                .OfType("TextField").WithDisplayName("Footer note").WithPosition("6")
                .WithEditor("TextArea")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Small print under the sections.",
                })));

        return 8;
    }

    /// <summary>
    /// Lets an editor size an individual block instead of always filling the
    /// section.
    ///
    /// The width is one shared part welded onto every block type, so the choice
    /// appears in the same place everywhere and a new block type gets it by
    /// being added to this list. Values are a fixed set of fractions rather
    /// than free numbers: a typed pixel width would overflow small screens,
    /// while a quarter of a row cannot.
    /// </summary>
    public async Task<int> UpdateFrom8Async()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync("BlockLayout", part => part
            .Attachable()
            .WithDescription("How much of the row this block occupies.")
            .WithField("Width", f => f
                .OfType("TextField").WithDisplayName("Block width").WithPosition("0")
                .WithSettings(PredefinedList(
                    "full", "Full width",
                    "half", "Half (1/2)",
                    "third", "One third (1/3)",
                    "two-thirds", "Two thirds (2/3)"))));

        foreach (var blockType in AllBlockTypes)
        {
            await _contentDefinitionManager.AlterTypeDefinitionAsync(blockType, type => type
                .WithPart("BlockLayout", p => p
                    .WithDisplayName("Layout")
                    .WithPosition("Parts:20#Layout;0")));
        }

        return 9;
    }

    /// <summary>
    /// Gives the newsletter a saved header and footer, and renames the campaign's
    /// masthead field to say what it is.
    ///
    /// A NewsletterSettings item holds the branding once — logo, colour, contact
    /// details, social links, the small print — so an editor is not retyping it
    /// on every issue. Anything left blank falls back to the site footer, which
    /// already carries the real brand name, email and phone.
    /// </summary>
    public async Task<int> UpdateFrom9Async()
    {
        // "Edition" read as a category; it is the line printed in the header.
        await _contentDefinitionManager.AlterPartDefinitionAsync("Campaign", part => part
            .WithField("Edition", f => f
                .OfType("TextField").WithDisplayName("Header line").WithPosition("5")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Shown in the newsletter header, e.g. JULY 2026. "
                        + "Leave blank to omit it.",
                })));

        await _contentDefinitionManager.AlterPartDefinitionAsync("NewsletterSettings", part => part
            .WithField("Logo", f => f
                .OfType("MediaField").WithDisplayName("Logo").WithPosition("0")
                .WithSettings(new MediaFieldSettings
                {
                    Multiple = false,
                    Hint = "Travels with every issue. Keep it under ~400px wide.",
                }))
            .WithField("AccentColour", f => f
                .OfType("TextField").WithDisplayName("Accent colour").WithPosition("1")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Hex value for the section bars, e.g. #1b3a6b. "
                        + "Blank uses the site's navy.",
                }))
            .WithField("Email", f => f
                .OfType("TextField").WithDisplayName("Contact email").WithPosition("2")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Blank falls back to the site footer's address.",
                }))
            .WithField("Phone", f => f
                .OfType("TextField").WithDisplayName("Phone").WithPosition("3"))
            .WithField("Address", f => f
                .OfType("TextField").WithDisplayName("Address").WithPosition("4"))
            .WithField("WebsiteUrl", f => f
                .OfType("TextField").WithDisplayName("Website").WithPosition("5"))
            .WithField("FooterText", f => f
                .OfType("TextField").WithDisplayName("Footer small print").WithPosition("6")
                .WithEditor("TextArea")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Why the reader is receiving this, and any legal line.",
                }))
            .WithField("UnsubscribeUrl", f => f
                .OfType("TextField").WithDisplayName("Unsubscribe link").WithPosition("7")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Required by most providers for bulk mail.",
                })));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("NewsletterSettings", type => type
            .DisplayedAs("Newsletter settings")
            .Creatable()
            .Securable()
            .WithPart(nameof(TitlePart), p => p
                .WithDisplayName("Name")
                .WithPosition("0"))
            .WithPart("NewsletterSettings", p => p.WithPosition("1"))
            .WithPart("SocialLinks", "BagPart", p => p
                .WithDisplayName("Social links")
                .WithPosition("2")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["SocialLink"],
                    DisplayType = "Detail",
                }))
            .WithSettings(new GraphQLContentTypeSettings { Hidden = true }));

        return 10;
    }

    /// <summary>
    /// Moves the contact block labels into the CMS.
    ///
    /// The field captions and the details headings were the last strings baked
    /// into the frontend's translation table, so an editor could change a phone
    /// number but not the word above it. They are ordinary fields now.
    ///
    /// Each is optional: a blank one falls back to the built-in wording, so a
    /// half-filled block still renders a complete form rather than bare inputs.
    /// </summary>
    public async Task<int> UpdateFrom10Async()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync("ContactFormBlock", part => part
            .WithField("NameLabel", f => f
                .OfType("TextField").WithDisplayName("\"Name\" label").WithPosition("5")
                .WithSettings(new TextFieldSettings { Hint = "Blank keeps the default wording." }))
            .WithField("EmailLabel", f => f
                .OfType("TextField").WithDisplayName("\"Email\" label").WithPosition("6"))
            .WithField("OrganisationLabel", f => f
                .OfType("TextField").WithDisplayName("\"Organisation\" label").WithPosition("7"))
            .WithField("MessageLabel", f => f
                .OfType("TextField").WithDisplayName("\"Message\" label").WithPosition("8")));

        await _contentDefinitionManager.AlterPartDefinitionAsync("ContactInfoBlock", part => part
            .WithField("EmailLabel", f => f
                .OfType("TextField").WithDisplayName("\"Email\" heading").WithPosition("5")
                .WithSettings(new TextFieldSettings { Hint = "Blank keeps the default wording." }))
            .WithField("PhoneLabel", f => f
                .OfType("TextField").WithDisplayName("\"Phone\" heading").WithPosition("6"))
            .WithField("AddressLabel", f => f
                .OfType("TextField").WithDisplayName("\"Address\" heading").WithPosition("7"))
            .WithField("HoursLabel", f => f
                .OfType("TextField").WithDisplayName("\"Working hours\" heading").WithPosition("8")));

        return 11;
    }

    /// <summary>
    /// Adds the photo and video galleries.
    ///
    /// Two separate blocks rather than one: a photo grid and a video list have
    /// different shapes — videos need a poster frame and play inline at a fixed
    /// aspect, photos tile and open in a lightbox — and keeping them apart lets
    /// an editor pick the right one instead of configuring a mode.
    ///
    /// MediaBlock stays as it is for a single standalone image.
    /// </summary>
    public async Task<int> UpdateFrom11Async()
    {
        await SimpleWidgetAsync("PhotoGalleryBlock", "Photo gallery", part => part
            .WithField("Heading", f => f
                .OfType("TextField").WithDisplayName("Heading").WithPosition("0"))
            .WithField("Lead", f => f
                .OfType("TextField").WithDisplayName("Lead paragraph").WithPosition("1")
                .WithEditor("TextArea"))
            .WithField("Images", f => f
                .OfType("MediaField").WithDisplayName("Images").WithPosition("2")
                .WithSettings(new MediaFieldSettings
                {
                    Multiple = true,
                    Hint = "Pick as many as you like — they tile automatically. "
                        + "The caption of each comes from its media text.",
                }))
            .WithField("Columns", f => f
                .OfType("TextField").WithDisplayName("Columns").WithPosition("3")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList(
                    "auto", "Automatic",
                    "2", "Two",
                    "3", "Three",
                    "4", "Four")))
            .WithField("Lightbox", f => f
                .OfType("BooleanField").WithDisplayName("Open full size on click").WithPosition("4")
                .WithSettings(new BooleanFieldSettings { DefaultValue = true })));

        await SimpleWidgetAsync("VideoGalleryBlock", "Video gallery", part => part
            .WithField("Heading", f => f
                .OfType("TextField").WithDisplayName("Heading").WithPosition("0"))
            .WithField("Lead", f => f
                .OfType("TextField").WithDisplayName("Lead paragraph").WithPosition("1")
                .WithEditor("TextArea"))
            .WithField("Columns", f => f
                .OfType("TextField").WithDisplayName("Columns").WithPosition("2")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList(
                    "auto", "Automatic",
                    "1", "One",
                    "2", "Two",
                    "3", "Three"))));

        // Each video is its own item: a poster and a title cannot be attached to
        // entries of a single multi-file media field.
        await SimpleWidgetAsync("VideoItem", "Video", part => part
            .WithField("Video", f => f
                .OfType("MediaField").WithDisplayName("Video file").WithPosition("0")
                .WithSettings(new MediaFieldSettings
                {
                    Multiple = false,
                    Hint = "An uploaded .mp4 or .webm. Leave blank if using a link.",
                }))
            .WithField("VideoUrl", f => f
                .OfType("TextField").WithDisplayName("Or video URL").WithPosition("1")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "A YouTube or Vimeo address, used when no file is uploaded.",
                }))
            .WithField("Poster", f => f
                .OfType("MediaField").WithDisplayName("Poster image").WithPosition("2")
                .WithSettings(new MediaFieldSettings
                {
                    Multiple = false,
                    Hint = "Shown before playback starts.",
                }))
            .WithField("Description", f => f
                .OfType("TextField").WithDisplayName("Description").WithPosition("3")
                .WithEditor("TextArea")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("VideoGalleryBlock", type => type
            .WithPart("Videos", "BagPart", p => p
                .WithDisplayName("Videos")
                .WithPosition("Parts:5#Videos;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["VideoItem"],
                    DisplayType = "Detail",
                })));

        // Both take the shared width setting, like every other block.
        foreach (var blockType in new[] { "PhotoGalleryBlock", "VideoGalleryBlock" })
        {
            await _contentDefinitionManager.AlterTypeDefinitionAsync(blockType, type => type
                .WithPart("BlockLayout", p => p
                    .WithDisplayName("Layout")
                    .WithPosition("Parts:20#Layout;0")));
        }

        // Make them addable to a section alongside the existing blocks.
        await _contentDefinitionManager.AlterTypeDefinitionAsync("Section", type => type
            .WithPart("Blocks", "BagPart", p => p
                .WithDisplayName("Blocks")
                .WithPosition("Parts:10#Blocks;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes =
                    [
                        .. AllBlockTypes,
                        "PhotoGalleryBlock",
                        "VideoGalleryBlock",
                    ],
                    DisplayType = "Detail",
                })));

        return 12;
    }

    /// <summary>
    /// Adds the form builder: a block holding a list of editor-defined fields.
    ///
    /// OrchardCore.Forms is enabled but not used here — it renders a server-side
    /// form that posts back to a Razor page, which cannot work against an
    /// Angular frontend reading GraphQL. These types describe the form instead,
    /// and the frontend renders it from that description.
    ///
    /// Submissions land in FormSubmission items, so they are readable in the
    /// admin like any other content rather than needing a bespoke screen.
    /// </summary>
    public async Task<int> UpdateFrom12Async()
    {
        await SimpleWidgetAsync("FormField", "Form field", part => part
            .WithField("FieldType", f => f
                .OfType("TextField").WithDisplayName("Field type").WithPosition("0")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList(
                    "text", "Single line text",
                    "email", "Email",
                    "tel", "Phone",
                    "number", "Number",
                    "textarea", "Paragraph",
                    "select", "Dropdown",
                    "checkbox", "Checkbox",
                    "date", "Date")))
            .WithField("Placeholder", f => f
                .OfType("TextField").WithDisplayName("Placeholder").WithPosition("1"))
            .WithField("Options", f => f
                .OfType("TextField").WithDisplayName("Dropdown options").WithPosition("2")
                .WithEditor("TextArea")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "One per line. Only used by the dropdown type.",
                }))
            .WithField("Required", f => f
                .OfType("BooleanField").WithDisplayName("Required").WithPosition("3"))
            .WithField("Width", f => f
                .OfType("TextField").WithDisplayName("Field width").WithPosition("4")
                .WithEditor("PredefinedList")
                .WithSettings(PredefinedList(
                    "full", "Full width",
                    "half", "Half width"))));

        await SimpleWidgetAsync("FormBlock", "Form", part => part
            .WithField("Heading", f => f
                .OfType("TextField").WithDisplayName("Heading").WithPosition("0"))
            .WithField("Intro", f => f
                .OfType("TextField").WithDisplayName("Intro").WithPosition("1")
                .WithEditor("TextArea"))
            .WithField("SubmitText", f => f
                .OfType("TextField").WithDisplayName("Submit button text").WithPosition("2"))
            .WithField("SuccessMessage", f => f
                .OfType("TextField").WithDisplayName("Success message").WithPosition("3")
                .WithEditor("TextArea"))
            .WithField("ErrorMessage", f => f
                .OfType("TextField").WithDisplayName("Error message").WithPosition("4")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Shown if the submission could not be delivered.",
                }))
            .WithField("NotifyEmail", f => f
                .OfType("TextField").WithDisplayName("Notify this address").WithPosition("5")
                .WithSettings(new TextFieldSettings
                {
                    Hint = "Each submission is emailed here. Blank stores it without notifying.",
                })));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("FormBlock", type => type
            .WithPart("Fields", "BagPart", p => p
                .WithDisplayName("Fields")
                .WithPosition("Parts:5#Fields;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes = ["FormField"],
                    DisplayType = "Detail",
                }))
            .WithPart("BlockLayout", p => p
                .WithDisplayName("Layout")
                .WithPosition("Parts:20#Layout;0")));

        // Submissions: listable so they can be read, but not creatable by hand —
        // they only ever arrive from the site.
        await _contentDefinitionManager.AlterPartDefinitionAsync("FormSubmission", part => part
            .WithField("FormName", f => f
                .OfType("TextField").WithDisplayName("Form").WithPosition("0"))
            .WithField("Answers", f => f
                .OfType("TextField").WithDisplayName("Answers").WithPosition("1")
                .WithEditor("TextArea"))
            .WithField("SubmittedAt", f => f
                .OfType("TextField").WithDisplayName("Received").WithPosition("2")));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("FormSubmission", type => type
            .DisplayedAs("Form submission")
            .Listable()
            .Securable()
            .WithPart(nameof(TitlePart), p => p
                .WithDisplayName("Reference")
                .WithPosition("0"))
            .WithPart("FormSubmission", p => p.WithPosition("1"))
            .WithSettings(new GraphQLContentTypeSettings { Hidden = true }));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("Section", type => type
            .WithPart("Blocks", "BagPart", p => p
                .WithDisplayName("Blocks")
                .WithPosition("Parts:10#Blocks;0")
                .WithSettings(new BagPartSettings
                {
                    ContainedContentTypes =
                    [
                        .. AllBlockTypes,
                        "PhotoGalleryBlock",
                        "VideoGalleryBlock",
                        "FormBlock",
                    ],
                    DisplayType = "Detail",
                })));

        return 13;
    }

    /// <summary>Every block an editor can place inside a section.</summary>
    private static readonly string[] AllBlockTypes =
    [
        "PageHeroBlock", "PageHeaderBlock", "RichTextBlock", "MediaBlock",
        "FeatureGridBlock", "ProductGridBlock", "ProductListBlock",
        "StoryBlock", "TimelineBlock", "ValuesBlock",
        "FaqBlock", "CtaBlock", "ContactFormBlock", "ContactInfoBlock",
    ];

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
