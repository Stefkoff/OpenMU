namespace MUnique.OpenMU.Web.PublicSite.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MUnique.OpenMU.Web.PublicSite.Models;
using MUnique.OpenMU.Web.PublicSite.Services;

/// <summary>
/// The admin-gated news management page: list, publish, edit and delete news entries.
/// </summary>
[Authorize]
public class ManageNewsModel : PageModel
{
    private readonly NewsService _newsService;
    private readonly IOptions<SiteSettings> _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManageNewsModel"/> class.
    /// </summary>
    public ManageNewsModel(NewsService newsService, IOptions<SiteSettings> settings)
    {
        this._newsService = newsService;
        this._settings = settings;
    }

    /// <summary>
    /// Gets the news entries, newest first.
    /// </summary>
    public IReadOnlyList<NewsItem> Items { get; private set; } = [];

    /// <summary>
    /// Gets or sets the title of a new article.
    /// </summary>
    [BindProperty]
    public string? CreateTitle { get; set; }

    /// <summary>
    /// Gets or sets the date of a new article (yyyy-MM-dd).
    /// </summary>
    [BindProperty]
    public string? CreateDate { get; set; }

    /// <summary>
    /// Gets or sets the body of a new article.
    /// </summary>
    [BindProperty]
    public string? CreateBody { get; set; }

    /// <inheritdoc/>
    public IActionResult OnGet()
    {
        if (!this.IsAdmin())
        {
            return this.Forbid();
        }

        this.Load();
        return this.Page();
    }

    /// <summary>
    /// Publishes a new article.
    /// </summary>
    public IActionResult OnPostCreate()
    {
        if (!this.IsAdmin())
        {
            return this.Forbid();
        }

        var title = this.CreateTitle;
        if (string.IsNullOrWhiteSpace(title))
        {
            this.ModelState.AddModelError(string.Empty, "The title is required.");
            this.Load();
            return this.Page();
        }

        var item = new NewsItem
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Date = this.NormalizedDate(this.CreateDate),
            Body = this.CreateBody ?? string.Empty,
        };

        if (this._newsService.TrySaveEntries(this._newsService.GetNews().Concat([item]).ToList()))
        {
            this.TempData["Message"] = "Article published.";
            return this.RedirectToPage();
        }

        this.ModelState.AddModelError(string.Empty, "The article could not be saved (file write failed).");
        this.Load();
        return this.Page();
    }

    /// <summary>
    /// Updates an existing article.
    /// </summary>
    public IActionResult OnPostEdit(Guid id, string? title, string? date, string? body)
    {
        if (!this.IsAdmin())
        {
            return this.Forbid();
        }

        var items = this._newsService.GetNews().ToList();
        var existing = items.FirstOrDefault(i => i.Id == id);
        if (existing is null)
        {
            this.ModelState.AddModelError(string.Empty, "The article no longer exists.");
        }
        else if (string.IsNullOrWhiteSpace(title))
        {
            this.ModelState.AddModelError(string.Empty, "The title is required.");
        }
        else
        {
            existing.Title = title.Trim();
            existing.Date = this.NormalizedDate(date);
            existing.Body = body ?? string.Empty;
            if (this._newsService.TrySaveEntries(items))
            {
                this.TempData["Message"] = "Article updated.";
                return this.RedirectToPage();
            }

            this.ModelState.AddModelError(string.Empty, "The article could not be saved (file write failed).");
        }

        this.Load();
        return this.Page();
    }

    /// <summary>
    /// Deletes an existing article.
    /// </summary>
    public IActionResult OnPostDelete(Guid id)
    {
        if (!this.IsAdmin())
        {
            return this.Forbid();
        }

        var items = this._newsService.GetNews().Where(i => i.Id != id).ToList();
        if (items.Count == this._newsService.GetNews().Count)
        {
            this.ModelState.AddModelError(string.Empty, "The article no longer exists.");
        }
        else if (this._newsService.TrySaveEntries(items))
        {
            this.TempData["Message"] = "Article deleted.";
            return this.RedirectToPage();
        }
        else
        {
            this.ModelState.AddModelError(string.Empty, "The article could not be deleted (file write failed).");
        }

        this.Load();
        return this.Page();
    }

    private void Load()
    {
        this.Items = this._newsService.GetNews();
    }

    private string NormalizedDate(string? date)
        => DateTime.TryParse(date, out var parsed) ? parsed.ToString("yyyy-MM-dd") : DateTime.UtcNow.ToString("yyyy-MM-dd");

    private bool IsAdmin()
    {
        var loginName = this.User.Identity?.Name;
        return loginName is not null
               && this._settings.Value.Admins.Contains(loginName, StringComparer.OrdinalIgnoreCase);
    }
}
