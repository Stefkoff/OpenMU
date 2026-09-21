namespace MUnique.OpenMU.Web.PublicSite.Models;

using System.Data.Common;

/// <summary>
/// The configurable settings of the public site.
/// </summary>
public sealed class SiteSettings
{
    /// <summary>
    /// Gets or sets the name of the server, used on the site.
    /// </summary>
    public string Name { get; set; } = "MuStefkoff";

    /// <summary>
    /// Gets or sets the tagline.
    /// </summary>
    public string Tagline { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the game server host within the docker network.
    /// </summary>
    public string GameHost { get; set; } = "openmu";

    /// <summary>
    /// Gets or sets the game server port used for the online/offline probe.
    /// </summary>
    public int GamePort { get; set; } = 55902;

    /// <summary>
    /// Gets or sets the URL of the public status endpoint of the game server.
    /// </summary>
    public string StatusEndpoint { get; set; } = "http://openmu:8080/api/public/status";

    /// <summary>
    /// Gets or sets the maximum number of rows shown in rankings.
    /// </summary>
    public int RankingSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the client download URL. Empty means not available yet.
    /// </summary>
    public string ClientDownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Linux client download URL. Empty means not available yet.
    /// </summary>
    public string ClientLinuxDownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the patch download URL. Empty means not available yet.
    /// </summary>
    public string PatchDownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the login names of accounts allowed to manage the site content (e.g. news).
    /// Configured via the Site:Admins section, commonly as env overrides like Site__Admins__0.
    /// </summary>
    public ICollection<string> Admins { get; set; } = [];
}

/// <summary>
/// Input of the account registration form.
/// </summary>
public sealed class RegisterInput
{
    /// <summary>
    /// Gets or sets the login name (3-10 letters/digits).
    /// </summary>
    public string? LoginName { get; set; }

    /// <summary>
    /// Gets or sets the password (3-20 characters).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the password confirmation.
    /// </summary>
    public string? ConfirmPassword { get; set; }

    /// <summary>
    /// Gets or sets the security code used in-game to confirm character deletion and guild kicks.
    /// </summary>
    public string? SecurityCode { get; set; }

    /// <summary>
    /// Gets or sets the optional e-mail address.
    /// </summary>
    public string? EMail { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the rules were accepted.
    /// </summary>
    public bool AgreeTerms { get; set; }
}

/// <summary>
/// Input of the login form.
/// </summary>
public sealed class LoginInput
{
    /// <summary>
    /// Gets or sets the login name.
    /// </summary>
    public string? LoginName { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// A row of the character rankings or the character list of an account.
/// </summary>
public sealed record CharacterStats(string CharacterName, string ClassName, int Level, int Resets, int MasterLevel)
{
    /// <summary>
    /// Maps a <see cref="DbDataReader"/> row.
    /// </summary>
    public static CharacterStats FromReader(DbDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4));
}

/// <summary>
/// A row of the guild ranking.
/// </summary>
public sealed record GuildRankRow(string GuildName, int Score, int Members)
{
    /// <summary>
    /// Maps a <see cref="DbDataReader"/> row.
    /// </summary>
    public static GuildRankRow FromReader(DbDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetInt32(2));
}

/// <summary>
/// A news entry.
/// </summary>
public sealed class NewsItem
{
    /// <summary>
    /// Gets or sets the stable id used for editing and deleting the entry.
    /// Entries from older files without an id get one assigned at load time.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date in yyyy-MM-dd format.
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the body text. Line breaks are preserved.
    /// </summary>
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// The current status of the game server, as shown on the site.
/// </summary>
public sealed class GameStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether the server is reachable.
    /// </summary>
    public bool Online { get; set; }

    /// <summary>
    /// Gets or sets the total number of connected players.
    /// </summary>
    public int? PlayerCount { get; set; }

    /// <summary>
    /// Gets or sets the names of the connected players.
    /// </summary>
    public List<string> PlayerNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the per-server status.
    /// </summary>
    public List<ServerStatus> Servers { get; set; } = [];

    /// <summary>
    /// Gets or sets the UTC timestamp of the check.
    /// </summary>
    public DateTime LastCheckedUtc { get; set; }

    /// <summary>
    /// Gets or sets an optional human-readable note.
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// The status of a single game server instance.
/// </summary>
public sealed class ServerStatus
{
    /// <summary>
    /// Gets or sets the server id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the server description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server state.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of connected players.
    /// </summary>
    public int Players { get; set; }
}
