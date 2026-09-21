namespace MUnique.OpenMU.Web.PublicSite.Services;

using System.IO;
using System.Text.Json;
using MUnique.OpenMU.Web.PublicSite.Models;

/// <summary>
/// Serves the news entries from a JSON file in the Data directory.
/// The file can be edited through the admin-gated news management page
/// (and manually on the server without rebuilding the site).
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
    /// Gets the absolute path of the news json file.
    /// </summary>
    public string FilePath => Path.Combine(this._environment.ContentRootPath, "Data", "news.json");

    // Files written by hand (and the seeded samples) use lowercase keys; the model
    // properties are PascalCase, so reads must be case-insensitive.
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Gets all news entries, newest first. Entries from older files without an id get one assigned.
    /// </summary>
    public IReadOnlyList<NewsItem> GetNews()
    {
        lock (this._lock)
        {
            if (this._cached is not null && DateTime.UtcNow - this._lastReadUtc < CacheDuration)
            {
                return this._cached;
            }
        }

        try
        {
            var items = File.Exists(this.FilePath)
                ? JsonSerializer.Deserialize<List<NewsItem>>(File.ReadAllText(this.FilePath), NewsService.ReadOptions) ?? []
                : [];
            foreach (var item in items)
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }
            }

            // stable sort: newest date first, insertion order preserved for equal dates
            items = items.OrderByDescending(i => i.Date).ToList();
            lock (this._lock)
            {
                this._cached = items;
                this._lastReadUtc = DateTime.UtcNow;
            }

            return items;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to read the news file {Path}", this.FilePath);
            return [];
        }
    }

    /// <summary>
    /// Saves the given entries to the news file. The write is atomic (temp file + rename)
    /// and the previous file content is kept as <c>news.json.bak</c>.
    /// </summary>
    /// <returns><c>True</c> when the file was written successfully.</returns>
    public bool TrySaveEntries(IReadOnlyList<NewsItem> items)
    {
        try
        {
            var path = this.FilePath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = path + ".tmp";
            var json = JsonSerializer.Serialize(items.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempPath, json);
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }

            File.Move(tempPath, path, overwrite: true);
            lock (this._lock)
            {
                this._cached = items.OrderByDescending(i => i.Date).ToList();
                this._lastReadUtc = DateTime.UtcNow;
            }

            return true;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to save the news file {Path}", this.FilePath);
            return false;
        }
    }
}
