using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using TrafoTech.Admin.Forms;
using TrafoTech.Admin.Migrations;
using TrafoTech.Admin.Newsletter;

namespace TrafoTech.Admin;

public sealed class Startup : StartupBase
{
    private readonly string _adminPrefix;

    public Startup(IConfiguration configuration)
    {
        _adminPrefix = configuration["OrchardCore:OrchardCore_Admin:AdminUrlPrefix"] ?? "Admin";
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        // Migrations run on every start and are versioned per class, so schema
        // changes apply to an existing database instead of requiring a rebuild.
        services.AddScoped<IDataMigration, ContentTypeMigrations>();
        services.AddScoped<IDataMigration, ContentMigrations>();

        // Keeps the admin left-to-right regardless of the visitor's culture cookie.
        services.AddSingleton<IConfigureOptions<RequestLocalizationOptions>>(
            new AdminCultureOptionsSetup(_adminPrefix));

        // Newsletter: resolves a campaign's audience and mails it. Admin-only —
        // nothing in the frontend queries or routes to these types.
        services.AddScoped<NewsletterRenderer>();
        services.AddScoped<CampaignSender>();
        services.AddScoped<IContentDisplayDriver, CampaignSendDriver>();
        services.AddScoped<INavigationProvider, NewsletterAdminMenu>();

        // Form submissions get their own sidebar entry so incoming requests are
        // not buried in the general Content Items list.
        services.AddScoped<INavigationProvider, FormSubmissionAdminMenu>();
    }

    public override void Configure(
        IApplicationBuilder builder,
        IEndpointRouteBuilder routes,
        IServiceProvider serviceProvider)
    {
        // Module controllers are not routed by convention, so the send endpoint
        // is mapped explicitly. It sits under the admin prefix and the
        // controller itself is [Admin] + [Authorize], so it is not public.
        routes.MapAreaControllerRoute(
            name: "TravotechCampaignSend",
            areaName: "Travotech.Admin",
            pattern: _adminPrefix + "/Campaign/Send",
            defaults: new { controller = "CampaignAdmin", action = "Send" });
    }
}
