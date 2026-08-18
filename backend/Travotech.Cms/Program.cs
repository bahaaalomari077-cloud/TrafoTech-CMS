using OrchardCore.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseNLogHost();

// Media uploads are chunked, but the request carrying each chunk still has to
// fit. Kestrel's own 30 MB default rejected video before Orchard's limit was
// ever consulted, so both are raised together — see OrchardCore_Media:MaxFileSize.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 524_288_000;
});

// Angular dev server + production origins, read from configuration.
var allowedOrigins = builder.Configuration
    .GetSection("TrafoTech:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("TrafoTechSpa", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services
    .AddOrchardCms();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Must sit before UseOrchardCore so the GraphQL/API endpoints inherit the policy.
app.UseCors("TrafoTechSpa");

app.UseOrchardCore();

app.Run();
