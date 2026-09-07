using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Models;

namespace OpenIddict.Management.Dashboard.Pages.Applications;

/// <summary>
/// Page model for application details view and single-action credential management.
/// </summary>
public class DetailsModel(IApplicationManagementService applicationService) : PageModel
{
    /// <summary>Gets or sets the loaded application domain model.</summary>
    public ManagedApplication Application { get; set; } = null!;

    /// <summary>Gets or sets feedback status message.</summary>
    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>Handles GET request for viewing application details.</summary>
    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var result = await applicationService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound();
        }

        Application = result.Value;
        return Page();
    }

    /// <summary>Handles POST request for updating / rotating client secret alone.</summary>
    public async Task<IActionResult> OnPostUpdateSecretAsync(string id, string? newSecret, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var result = await applicationService.UpdateClientSecretAsync(id, newSecret, cancellationToken);
        if (result.IsSuccess)
        {
            StatusMessage = "Client secret updated successfully.";
        }
        else
        {
            StatusMessage = $"Failed to update client secret: {result.Error?.Description}";
        }

        return RedirectToPage(new { id });
    }
}
