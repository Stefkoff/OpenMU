namespace MUnique.OpenMU.Web.PublicSite.Services;

using Microsoft.EntityFrameworkCore;
using MUnique.OpenMU.Persistence.EntityFramework;

/// <summary>
/// Provides the database connection settings for all OpenMU EF contexts from environment variables.
/// The website connects with a dedicated, limited-privilege role - it can read the game data
/// (accounts, characters, guilds, configuration) and write only accounts, never game configuration.
/// </summary>
public sealed class EnvDatabaseConnectionSettingProvider : IDatabaseConnectionSettingProvider
{
    private readonly ConnectionSetting _setting;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvDatabaseConnectionSettingProvider"/> class.
    /// </summary>
    public EnvDatabaseConnectionSettingProvider()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "webapp";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
                       ?? throw new InvalidOperationException("The DB_PASSWORD environment variable is not set.");
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "openmu";
        this._setting = new ConnectionSetting
        {
            ConnectionString = $"Server={host};Port={port};User Id={user};Password={password};Database={database};Command Timeout=60;",
            DatabaseEngine = DatabaseEngine.Npgsql,
        };
    }

    /// <inheritdoc />
    public Task? Initialization { get; private set; }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        this.Initialization = Task.CompletedTask;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ConnectionSetting GetConnectionSetting<TContextType>()
        where TContextType : DbContext => this._setting;

    /// <inheritdoc />
    public ConnectionSetting GetConnectionSetting(Type contextType) => this._setting;
}
