namespace MUnique.OpenMU.Web.PublicSite.Pages;

using Microsoft.AspNetCore.Mvc.RazorPages;
using MUnique.OpenMU.Web.PublicSite.Models;
using MUnique.OpenMU.Web.PublicSite.Services;

/// <summary>
/// The home page: server status, rates and latest news.
/// </summary>
public class IndexModel : PageModel
{
    private readonly GameStatusService _statusService;
    private readonly NewsService _newsService;
    private readonly PersistenceFactory _persistenceFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    public IndexModel(GameStatusService statusService, NewsService newsService, PersistenceFactory persistenceFactory, IOptions<SiteSettings> settings)
    {
        this._statusService = statusService;
        this._newsService = newsService;
        this._persistenceFactory = persistenceFactory;
        this.Settings = settings.Value;
    }

    /// <summary>
    /// Gets the site settings.
    /// </summary>
    public SiteSettings Settings { get; }

    /// <summary>
    /// Gets the current server status.
    /// </summary>
    public GameStatus? Status { get; private set; }

    /// <summary>
    /// Gets the latest news.
    /// </summary>
    public IReadOnlyList<NewsItem> LatestNews { get; private set; } = [];

    /// <summary>
    /// Gets the experience rate.
    /// </summary>
    public float? ExperienceRate { get; private set; }

    /// <summary>
    /// Gets the master experience rate.
    /// </summary>
    public float? MasterExperienceRate { get; private set; }

    /// <summary>
    /// Gets or sets the total level cap.
    /// </summary>
    public int? MaximumLevel { get; private set; }

    /// <inheritdoc/>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        this.Status = await this._statusService.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        this.LatestNews = this._newsService.GetNews().Take(3).ToList();

        try
        {
            var config = await this._persistenceFactory.GetGameConfigurationAsync(cancellationToken).ConfigureAwait(false);
            this.ExperienceRate = config.ExperienceRate;
            this.MasterExperienceRate = config.MasterExperienceRate;
            this.MaximumLevel = config.MaximumLevel;
        }
        catch (Exception)
        {
            // Rates stay null if the configuration is not reachable yet.
        }
    }
}
