namespace MUnique.OpenMU.Web.PublicSite.Services;

using System.IO;
using System.Text.Json;
using MUnique.OpenMU.Web.PublicSite.Models;

/// <summary>
/// Serves the news entries from a JSON file in the Data directory.
/// The file can be edited on the server without rebuilding the site.
/// </summary>
public sealed class NewsService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<NewsService> _logger;
    private readonly object _lock = new();
    private IReadOnlyList<NewsItem>? _cached;
    private DateTime _lastReadUtc = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsService"/> class.
    /// </summary>
    public NewsService(IWebHostEnvironment environment, ILogger<NewsService> logger)
    {
        this._environment = environment;
        this._logger = logger;
    }

    /// <summary>
    /// Gets all news entries, newest first.
    /// </summary>
    public IReadOnlyList<NewsItem> GetNews()
    {
        if (this._cached is not null && DateTime.UtcNow - this._lastReadUtc < CacheDuration)
        {
            return this._cached;
        }

        var path = Path.Combine(this._environment.ContentRootPath, "Data", "news.json");
        try
        {
            var items = File.Exists(path)
                ? JsonSerializer.Deserialize<List<NewsItem>>(File.ReadAllText(path)) ?? []
                : [];
            items.Sort((a, b) => string.CompareOrdinal(b.Date, a.Date));
            lock (this._lock)
            {
                this._cached = items;
                this._lastReadUtc = DateTime.UtcNow;
            }

            return items;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to read the news file {Path}", path);
            return [];
        }
    }
}
