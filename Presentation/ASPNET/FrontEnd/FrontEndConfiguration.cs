using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

namespace ASPNET.FrontEnd;

public static class FrontEndConfiguration
{
    // Same cookie name as CookieRequestCultureProvider default (not a C# const).
    public static readonly string CultureCookieName = CookieRequestCultureProvider.DefaultCookieName;

    public static IServiceCollection AddFrontEndServices(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = new[] { "ar", "en" }.Select(static c => new CultureInfo(c)).ToList();
            options.DefaultRequestCulture = new RequestCulture("ar");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.RequestCultureProviders =
            [
                new CookieRequestCultureProvider { CookieName = CultureCookieName },
                new AcceptLanguageHeaderRequestCultureProvider(),
            ];
        });

        var razorPages = services.AddRazorPages(options =>
        {
            options.RootDirectory = "/FrontEnd/Pages";
        });

        if (string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Environments.Development,
                StringComparison.OrdinalIgnoreCase))
        {
            razorPages.AddRazorRuntimeCompilation();
        }

        razorPages
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();

        return services;
    }

    public static IEndpointRouteBuilder MapFrontEndRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/culture/set", (HttpContext http, string lang) =>
        {
            var cultureName = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ar";
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture));
            http.Response.Cookies.Append(
                CultureCookieName,
                cookieValue,
                new CookieOptions
                {
                    Path = "/",
                    MaxAge = TimeSpan.FromDays(365),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = false,
                });
            return Results.Ok(new { culture = cultureName });
        }).AllowAnonymous();

        endpoints.MapRazorPages()
            .WithStaticAssets();
        return endpoints;
    }
}
