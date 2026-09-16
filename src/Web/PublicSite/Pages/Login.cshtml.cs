namespace MUnique.OpenMU.Web.PublicSite.Pages;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MUnique.OpenMU.Web.PublicSite.Models;
using MUnique.OpenMU.Web.PublicSite.Services;

/// <summary>
/// The login page for the game account.
/// </summary>
public class LoginModel : PageModel
{
    private readonly SiteAccountService _accountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginModel"/> class.
    /// </summary>
    public LoginModel(SiteAccountService accountService)
    {
        this._accountService = accountService;
    }

    /// <summary>
    /// Gets or sets the login input.
    /// </summary>
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    /// <inheritdoc/>
    public IActionResult OnGet(string? returnUrl = null)
    {
        if (this.User.Identity?.IsAuthenticated == true)
        {
            return this.RedirectToPage("/Account");
        }

        this.ReturnUrl = returnUrl;
        return this.Page();
    }

    /// <summary>
    /// Gets or sets the return URL.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Handles the sign-in.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        var account = await this._accountService.TryLoginAsync(this.Input.LoginName ?? string.Empty, this.Input.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            this.ModelState.AddModelError(string.Empty, "Invalid login name or password.");
            return this.Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, account.LoginName),
            new(ClaimTypes.NameIdentifier, ((MUnique.OpenMU.Persistence.IIdentifiable)account).Id.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await this.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(returnUrl) && this.Url.IsLocalUrl(returnUrl))
        {
            return this.Redirect(returnUrl);
        }

        return this.RedirectToPage("/Account");
    }
}
