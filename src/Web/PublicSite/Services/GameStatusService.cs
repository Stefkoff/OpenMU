namespace MUnique.OpenMU.Web.PublicSite.Services;

using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using MUnique.OpenMU.Web.PublicSite.Models;

/// <summary>
/// Provides the current server status: online/offline, player count and player names.
/// Fetched from the game server's public status endpoint, with short-term caching.
/// </summary>
public sealed class GameStatusService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<SiteSettings> _settings;
    private readonly ILogger<GameStatusService> _logger;
    private readonly object _lock = new();
    private GameStatus? _cached;
    private DateTime _lastFetchUtc = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameStatusService"/> class.
    /// </summary>
    public GameStatusService(IHttpClientFactory httpClientFactory, IOptions<SiteSettings> settings, ILogger<GameStatusService> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._settings = settings;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the current server status.
    /// </summary>
    public async Task<GameStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        if (this._cached is not null && DateTime.UtcNow - this._lastFetchUtc < CacheDuration)
        {
            return this._cached;
        }

        GameStatus status;
        try
        {
            var endpoint = this._settings.Value.StatusEndpoint;
            var client = this._httpClientFactory.CreateClient("status");
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetFromJsonAsync<StatusResponse>(endpoint, cancellationToken).ConfigureAwait(false);
            status = new GameStatus
            {
                Online = response?.TotalPlayers >= 0,
                PlayerCount = response?.TotalPlayers,
                PlayerNames = response?.Players ?? [],
                Servers = response?.Servers ?? [],
                LastCheckedUtc = DateTime.UtcNow,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            this._logger.LogWarning(ex, "Status endpoint not reachable; falling back to port probe.");
            status = new GameStatus
            {
                Online = await this.ProbeGamePortAsync(cancellationToken).ConfigureAwait(false),
                LastCheckedUtc = DateTime.UtcNow,
                Note = "Status endpoint unavailable",
            };
        }

        lock (this._lock)
        {
            this._cached = status;
            this._lastFetchUtc = DateTime.UtcNow;
        }

        return status;
    }

    private async Task<bool> ProbeGamePortAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            var settings = this._settings.Value;
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(settings.GameHost, settings.GamePort, timeoutCts.Token).ConfigureAwait(false);
            return client.Connected;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return false;
        }
    }

    private sealed class StatusResponse
    {
        [JsonPropertyName("totalPlayers")]
        public int TotalPlayers { get; set; }

        [JsonPropertyName("players")]
        public List<string> Players { get; set; } = [];

        [JsonPropertyName("servers")]
        public List<ServerStatus> Servers { get; set; } = [];
    }
}
