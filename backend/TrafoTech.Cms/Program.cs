using OrchardCore.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseNLogHost();

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
