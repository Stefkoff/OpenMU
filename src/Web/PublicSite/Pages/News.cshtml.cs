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

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsModel"/> class.
    /// </summary>
    public NewsModel(NewsService newsService)
    {
        this._newsService = newsService;
    }

    /// <summary>
    /// Gets the news entries.
    /// </summary>
    public IReadOnlyList<NewsItem> Items { get; private set; } = [];

    /// <inheritdoc/>
    public void OnGet()
    {
        this.Items = this._newsService.GetNews();
    }
}
