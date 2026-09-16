namespace MUnique.OpenMU.Web.PublicSite.Pages;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MUnique.OpenMU.Web.PublicSite.Models;
using MUnique.OpenMU.Web.PublicSite.Services;

/// <summary>
/// The account overview: character list and password/security-code management.
/// </summary>
[Authorize]
public class AccountModel : PageModel
{
    private readonly SiteAccountService _accountService;
    private readonly RankingService _rankingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountModel"/> class.
    /// </summary>
    public AccountModel(SiteAccountService accountService, RankingService rankingService)
    {
        this._accountService = accountService;
        this._rankingService = rankingService;
    }

    /// <summary>
    /// Gets the login name of the signed-in account.
    /// </summary>
    public string? LoginName { get; private set; }

    /// <summary>
    /// Gets the characters of the account.
    /// </summary>
    public List<CharacterStats> Characters { get; private set; } = [];

    /// <summary>
    /// Gets or sets the current password.
    /// </summary>
    [BindProperty]
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    [BindProperty]
    public string? NewPassword { get; set; }

    /// <summary>
    /// Gets or sets the confirmation of the new password.
    /// </summary>
    [BindProperty]
    public string? ConfirmNewPassword { get; set; }

    /// <summary>
    /// Gets or sets the current security code.
    /// </summary>
    [BindProperty]
    public string? CurrentSecurityCode { get; set; }

    /// <summary>
    /// Gets or sets the new security code.
    /// </summary>
    [BindProperty]
    public string? NewSecurityCode { get; set; }

    /// <inheritdoc/>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var accountId = this.GetAccountId();
        this.LoginName = await this._accountService.GetLoginNameAsync(accountId, cancellationToken).ConfigureAwait(false);
        this.Characters = await this._rankingService.GetCharactersOfAccountAsync(accountId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the password change.
    /// </summary>
    public async Task<IActionResult> OnPostChangePasswordAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(this.NewPassword, this.ConfirmNewPassword, StringComparison.Ordinal))
        {
            this.ModelState.AddModelError(string.Empty, "The new passwords do not match.");
        }

        if (this.ModelState.IsValid)
        {
            var (success, error) = await this._accountService.ChangePasswordAsync(this.GetAccountId(), this.CurrentPassword ?? string.Empty, this.NewPassword ?? string.Empty, cancellationToken).ConfigureAwait(false);
            if (!success)
            {
                this.ModelState.AddModelError(string.Empty, error ?? "Password change failed.");
            }
        }

        if (!this.ModelState.IsValid)
        {
            await this.OnGetAsync(cancellationToken).ConfigureAwait(false);
            return this.Page();
        }

        this.TempData["Message"] = "Password changed.";
        return this.RedirectToPage();
    }

    /// <summary>
    /// Handles the security code change.
    /// </summary>
    public async Task<IActionResult> OnPostChangeSecurityCodeAsync(CancellationToken cancellationToken)
    {
        var (success, error) = await this._accountService.ChangeSecurityCodeAsync(this.GetAccountId(), this.CurrentSecurityCode ?? string.Empty, this.NewSecurityCode ?? string.Empty, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            this.ModelState.AddModelError(string.Empty, error ?? "Security code change failed.");
            await this.OnGetAsync(cancellationToken).ConfigureAwait(false);
            return this.Page();
        }

        this.TempData["Message"] = "Security code changed.";
        return this.RedirectToPage();
    }

    private Guid GetAccountId()
    {
        var value = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
