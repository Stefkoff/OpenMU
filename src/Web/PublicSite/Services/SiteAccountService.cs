namespace MUnique.OpenMU.Web.PublicSite.Services;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.PublicSite.Models;

/// <summary>
/// Account operations for the public site: registration, login and password/security-code changes.
/// Registration mirrors the admin panel account creation (BCrypt, State = Normal, UTC registration date).
/// </summary>
public sealed class SiteAccountService
{
    private readonly PersistenceFactory _persistenceFactory;
    private readonly ILogger<SiteAccountService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SiteAccountService"/> class.
    /// </summary>
    public SiteAccountService(PersistenceFactory persistenceFactory, ILogger<SiteAccountService> logger)
    {
        this._persistenceFactory = persistenceFactory;
        this._logger = logger;
    }

    /// <summary>
    /// Tries to log in the account with the given credentials.
    /// </summary>
    public async Task<Account?> TryLoginAsync(string loginName, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var config = await this._persistenceFactory.GetGameConfigurationAsync(cancellationToken).ConfigureAwait(false);
        using var context = this._persistenceFactory.Provider.CreateNewPlayerContext(config);
        var account = await context.GetAccountByLoginNameAsync(loginName, password, cancellationToken).ConfigureAwait(false);
        if (account is null || account.State is AccountState.Banned or AccountState.TemporarilyBanned)
        {
            return null;
        }

        return account;
    }

    /// <summary>
    /// Registers a new game account. Validation mirrors the admin panel: login 3-10, password 3-20, security code 3-10.
    /// </summary>
    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterInput input, CancellationToken cancellationToken)
    {
        var loginName = input.LoginName?.Trim();
        var password = input.Password;
        var securityCode = input.SecurityCode;
        if (string.IsNullOrWhiteSpace(loginName) || loginName.Length is < 3 or > 10)
        {
            return (false, "The login name must be 3 to 10 characters long.");
        }

        if (!loginName.All(char.IsAsciiLetterOrDigit))
        {
            return (false, "The login name may only contain letters and numbers.");
        }

        if (string.IsNullOrEmpty(password) || password.Length is < 3 or > 20)
        {
            return (false, "The password must be 3 to 20 characters long.");
        }

        if (string.IsNullOrEmpty(securityCode) || securityCode.Length is < 3 or > 10)
        {
            return (false, "The security code must be 3 to 10 characters long.");
        }

        var config = await this._persistenceFactory.GetGameConfigurationAsync(cancellationToken).ConfigureAwait(false);
        using var playerContext = this._persistenceFactory.Provider.CreateNewPlayerContext(config);
        var existing = await playerContext.GetAccountByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return (false, "This login name is already registered.");
        }

        using var context = this._persistenceFactory.Provider.CreateNewContext();
        var account = context.CreateNew<Account>();
        account.LoginName = loginName;
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        account.EMail = input.EMail?.Trim() ?? string.Empty;
        account.State = AccountState.Normal;
        account.SecurityCode = securityCode;
        account.RegistrationDate = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this._logger.LogInformation("New account registered: {LoginName}", loginName);
        return (true, null);
    }

    /// <summary>
    /// Changes the password of an account after verifying the current one.
    /// </summary>
    public async Task<(bool Success, string? Error)> ChangePasswordAsync(Guid accountId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length is < 3 or > 20)
        {
            return (false, "The new password must be 3 to 20 characters long.");
        }

        using var context = this._persistenceFactory.Provider.CreateNewContext();
        var account = (await context.GetAsync<Account>(cancellationToken).ConfigureAwait(false)).FirstOrDefault(a => a is MUnique.OpenMU.Persistence.IIdentifiable identifiable && identifiable.Id == accountId);
        if (account is null)
        {
            return (false, "The account was not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, account.PasswordHash))
        {
            return (false, "The current password is not correct.");
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this._logger.LogInformation("Password changed for account: {AccountId}", accountId);
        return (true, null);
    }

    /// <summary>
    /// Changes the security code of an account after verifying the current one.
    /// </summary>
    public async Task<(bool Success, string? Error)> ChangeSecurityCodeAsync(Guid accountId, string currentSecurityCode, string newSecurityCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(newSecurityCode) || newSecurityCode.Length is < 3 or > 10)
        {
            return (false, "The new security code must be 3 to 10 characters long.");
        }

        using var context = this._persistenceFactory.Provider.CreateNewContext();
        var account = (await context.GetAsync<Account>(cancellationToken).ConfigureAwait(false)).FirstOrDefault(a => a is MUnique.OpenMU.Persistence.IIdentifiable identifiable && identifiable.Id == accountId);
        if (account is null)
        {
            return (false, "The account was not found.");
        }

        if (!string.Equals(account.SecurityCode, currentSecurityCode, StringComparison.Ordinal))
        {
            return (false, "The current security code is not correct.");
        }

        account.SecurityCode = newSecurityCode;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this._logger.LogInformation("Security code changed for account: {AccountId}", accountId);
        return (true, null);
    }

    /// <summary>
    /// Gets the login name of an account.
    /// </summary>
    public async Task<string?> GetLoginNameAsync(Guid accountId, CancellationToken cancellationToken)
    {
        using var context = this._persistenceFactory.Provider.CreateNewContext();
        return (await context.GetAsync<Account>(cancellationToken).ConfigureAwait(false))
            .Where(a => a is MUnique.OpenMU.Persistence.IIdentifiable identifiable && identifiable.Id == accountId)
            .Select(a => a.LoginName)
            .FirstOrDefault();
    }
}
