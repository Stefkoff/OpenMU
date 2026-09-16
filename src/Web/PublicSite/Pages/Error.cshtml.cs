namespace MUnique.OpenMU.Web.PublicSite.Pages;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The error page.
/// </summary>
public class ErrorModel : PageModel
{
    /// <summary>
    /// Gets the request id.
    /// </summary>
    public string? RequestId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the request id should be shown.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);

    /// <inheritdoc/>
    public void OnGet()
    {
        this.RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier;
    }
}
