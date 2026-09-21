namespace MUnique.OpenMU.Web.PublicSite.Pages;

using Microsoft.AspNetCore.Mvc.RazorPages;
using MUnique.OpenMU.Web.PublicSite.Models;
using MUnique.OpenMU.Web.PublicSite.Services;

/// <summary>
/// The news page.
/// </summary>
public class NewsModel : PageModel
{
    private readonly NewsService _newsService;
    private readonly IOptions<SiteSettings> _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsModel"/> class.
    /// </summary>
    public NewsModel(NewsService newsService, IOptions<SiteSettings> settings)
    {
        this._newsService = newsService;
        this._settings = settings;
    }

    /// <summary>
    /// Gets the news entries.
    /// </summary>
    public IReadOnlyList<NewsItem> Items { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether the signed-in account may manage the news.
    /// </summary>
    public bool IsAdmin { get; private set; }

    /// <inheritdoc/>
    public void OnGet()
    {
        this.Items = this._newsService.GetNews();
        var loginName = this.User.Identity?.Name;
        this.IsAdmin = loginName is not null
                       && this._settings.Value.Admins.Contains(loginName, StringComparer.OrdinalIgnoreCase);
    }
}
