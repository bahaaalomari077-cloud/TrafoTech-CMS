using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace TrafoTech.Admin;

/// <summary>
/// Pins the admin UI to English.
///
/// TheAdmin renders its layout with dir="@Orchard.CultureDir()", so the panel's
/// direction follows the request culture. That culture comes from the visitor's
/// .AspNetCore.Culture cookie, which means an editor who once viewed the site in
/// Arabic gets a right-to-left admin — mirrored sidebar and all — even though
/// its strings are English.
///
/// Inserting this provider ahead of the cookie provider settles the culture for
/// /admin requests before anything reads it. Front-end requests are untouched,
/// so the public site still switches language normally.
/// </summary>
public sealed class AdminCultureProvider : RequestCultureProvider
{
    private static readonly Task<ProviderCultureResult?> English =
        Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult("en", "en"));

    private readonly string _adminPrefix;

    public AdminCultureProvider(string adminPrefix)
    {
        _adminPrefix = "/" + adminPrefix.Trim('/');
    }

    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var path = httpContext.Request.Path;

        // The login and setup screens are part of the admin experience even
        // though they sit outside the admin prefix.
        var isAdmin =
            path.StartsWithSegments(_adminPrefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/Login", StringComparison.OrdinalIgnoreCase);

        return isAdmin ? English : NullProviderCultureResult;
    }
}

/// <summary>
/// Registers <see cref="AdminCultureProvider"/> first, so it wins over the
/// cookie and Accept-Language providers.
/// </summary>
public sealed class AdminCultureOptionsSetup : IConfigureOptions<RequestLocalizationOptions>
{
    private readonly string _adminPrefix;

    public AdminCultureOptionsSetup(string adminPrefix)
    {
        _adminPrefix = adminPrefix;
    }

    public void Configure(RequestLocalizationOptions options)
    {
        options.RequestCultureProviders.Insert(0, new AdminCultureProvider(_adminPrefix));
    }
}
