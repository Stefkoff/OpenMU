namespace MUnique.OpenMU.Web.PublicSite.Services;

using Microsoft.EntityFrameworkCore.Metadata;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.EntityFramework;

/// <summary>
/// Initializes and holds the shared persistence infrastructure used by the public site.
/// </summary>
public sealed class PersistenceFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<PersistenceFactory> _logger;
    private readonly object _initLock = new();
    private PersistenceContextProvider? _provider;
    private GameConfiguration? _gameConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceFactory"/> class.
    /// </summary>
    public PersistenceFactory(ILoggerFactory loggerFactory, ILogger<PersistenceFactory> logger)
    {
        this._loggerFactory = loggerFactory;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the initialized persistence context provider.
    /// </summary>
    public PersistenceContextProvider Provider => this._provider ?? throw new InvalidOperationException("Persistence is not initialized.");

    /// <summary>
    /// Ensures the persistence infrastructure is initialized and the game configuration is loaded.
    /// </summary>
    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        lock (this._initLock)
        {
            if (this._provider is not null)
            {
                return this.TryLoadGameConfigurationAsync(cancellationToken);
            }

            if (!ConnectionConfigurator.IsInitialized)
            {
                var provider = new EnvDatabaseConnectionSettingProvider();
                ConnectionConfigurator.Initialize(provider);
            }

            this._provider = new PersistenceContextProvider(this._loggerFactory, new NoOpConfigurationChangeListener());
        }

        return this.TryLoadGameConfigurationAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the current game configuration.
    /// </summary>
    public async Task<GameConfiguration> GetGameConfigurationAsync(CancellationToken cancellationToken = default)
    {
        await this.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        return this._gameConfiguration ?? throw new InvalidOperationException("The game configuration could not be loaded.");
    }

    private async Task TryLoadGameConfigurationAsync(CancellationToken cancellationToken)
    {
        if (this._gameConfiguration is not null)
        {
            return;
        }

        try
        {
            using var context = this.Provider.CreateNewConfigurationContext();
            var configId = await context.GetDefaultGameConfigurationIdAsync(cancellationToken).ConfigureAwait(false);
            if (configId is { } id)
            {
                var configs = await context.GetAsync<GameConfiguration>(cancellationToken).ConfigureAwait(false);
                this._gameConfiguration = configs.FirstOrDefault(c => c is MUnique.OpenMU.Persistence.IIdentifiable identifiable && identifiable.Id == id);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to load the game configuration.");
        }
    }

    private sealed class NoOpConfigurationChangeListener : IConfigurationChangeListener
    {
        public ValueTask ConfigurationChangedAsync(Type type, Guid id, object configuration, object? parent) => ValueTask.CompletedTask;

        public ValueTask ConfigurationAddedAsync(Type type, Guid id, object configuration, object? parent, INavigationBase? parentCollectionNavigation) => ValueTask.CompletedTask;

        public ValueTask ConfigurationRemovedAsync(Type type, Guid id, object? parent, INavigationBase? parentCollectionNavigation) => ValueTask.CompletedTask;
    }
}
