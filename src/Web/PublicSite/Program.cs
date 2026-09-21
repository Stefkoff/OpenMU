namespace MUnique.OpenMU.Web.PublicSite;

using Microsoft.AspNetCore.Authentication.Cookies;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.EntityFramework.Json;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Web.PublicSite.Models;
using MUnique.OpenMU.Web.PublicSite.Services;

/// <summary>
/// The entry point of the public player website.
/// </summary>
public class Program
{
    /// <summary>
    /// Defines the entry point of the application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    public static void Main(string[] args)
    {
        // The game configuration JSON (rebuilt by the json query) encodes postgres
        // bytea values as \x hex - register the hex converter like Startup does,
        // otherwise the default Base64 converter throws on every byte[] token.
        JsonConverterRegistry.RegisterConverter(new LocalizedStringJsonConverter());
        JsonConverterRegistry.RegisterConverter(new BinaryAsHexJsonConverter());

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();
        builder.Services.AddHttpClient();
        builder.Services.AddAntiforgery();

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.LogoutPath = "/Logout";
                options.AccessDeniedPath = "/Login";
                options.Cookie.Name = "mustefkoff-web";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(12);
                options.SlidingExpiration = true;
            });
        builder.Services.AddAuthorization();

        builder.Services.AddOptions<SiteSettings>().Bind(builder.Configuration.GetSection("Site"));
        builder.Services.AddSingleton<PersistenceFactory>();
        builder.Services.AddScoped<SiteAccountService>();
        builder.Services.AddScoped<RankingService>();
        builder.Services.AddSingleton<GameStatusService>();
        builder.Services.AddSingleton<NewsService>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRazorPages();
        app.Run();
    }
}
