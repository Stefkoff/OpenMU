namespace MUnique.OpenMU.Web.PublicSite.Pages;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Signs the user out.
/// </summary>
public class LogoutModel : PageModel
{
    /// <summary>
    /// Handles the sign-out.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        return this.RedirectToPage("/Index");
    }
}
