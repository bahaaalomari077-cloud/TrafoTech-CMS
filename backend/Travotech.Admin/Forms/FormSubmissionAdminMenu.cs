using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace TrafoTech.Admin.Forms;

/// <summary>
/// Gives form submissions their own place in the admin sidebar.
///
/// Without this, every submission lands in the same Content Items list as the
/// site's pages and campaigns, which buries them. The entry links to the
/// content list pre-filtered to the FormSubmission type, so the incoming
/// requests have a screen of their own.
/// </summary>
public sealed class FormSubmissionAdminMenu : INavigationProvider
{
    private readonly IStringLocalizer S;
    private readonly string _adminPrefix;

    public FormSubmissionAdminMenu(
        IStringLocalizer<FormSubmissionAdminMenu> localizer,
        IConfiguration configuration)
    {
        S = localizer;
        _adminPrefix = configuration["OrchardCore:OrchardCore_Admin:AdminUrlPrefix"] ?? "Admin";
    }

    public ValueTask BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.CompletedTask;
        }

        builder.Add(S["Form Submissions"], "4.5", submissions => submissions
            .AddClass("form-submissions")
            .Id("formSubmissions")
            .Url($"~/{_adminPrefix}/Contents/ContentItems?contentTypeId=FormSubmission")
            .LocalNav());

        return ValueTask.CompletedTask;
    }
}
