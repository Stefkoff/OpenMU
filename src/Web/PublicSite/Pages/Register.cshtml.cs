namespace MUnique.OpenMU.Web.PublicSite.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MUnique.OpenMU.Web.PublicSite.Models;
using MUnique.OpenMU.Web.PublicSite.Services;

/// <summary>
/// The account registration page.
/// </summary>
public class RegisterModel : PageModel
{
    private readonly SiteAccountService _accountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterModel"/> class.
    /// </summary>
    public RegisterModel(SiteAccountService accountService)
    {
        this._accountService = accountService;
    }

    /// <summary>
    /// Gets or sets the registration input.
    /// </summary>
    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    /// <summary>
    /// Handles the registration.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!this.Input.AgreeTerms)
        {
            this.ModelState.AddModelError(string.Empty, "You have to accept the server rules.");
        }

        if (!string.Equals(this.Input.Password, this.Input.ConfirmPassword, StringComparison.Ordinal))
        {
            this.ModelState.AddModelError(string.Empty, "The passwords do not match.");
        }

        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        var (success, error) = await this._accountService.RegisterAsync(this.Input, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            this.ModelState.AddModelError(string.Empty, error ?? "Registration failed.");
            return this.Page();
        }

        this.TempData["Message"] = "Account created successfully - you can now sign in.";
        return this.RedirectToPage("/Login");
    }
}
