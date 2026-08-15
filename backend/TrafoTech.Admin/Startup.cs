using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using TrafoTech.Admin.Migrations;

namespace TrafoTech.Admin;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Migrations run on every start and are versioned per class, so schema
        // changes apply to an existing database instead of requiring a rebuild.
        services.AddScoped<IDataMigration, ContentTypeMigrations>();
        services.AddScoped<IDataMigration, ContentMigrations>();
    }
}
