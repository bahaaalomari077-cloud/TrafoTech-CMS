using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using TrafoTech.Admin.Migrations;

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
    }
}
