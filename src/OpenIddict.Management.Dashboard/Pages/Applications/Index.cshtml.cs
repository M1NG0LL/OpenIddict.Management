using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Dashboard.Pages.Applications;

/// <summary>
/// Page model for application listing, bulk operations, and management.
/// </summary>
public class IndexModel(IApplicationManagementService applicationService) : PageModel
{
    /// <summary>Gets or sets requested page index.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Gets or sets page size.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    /// <summary>Gets or sets optional search term.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>Gets or sets optional status filter.</summary>
    [BindProperty(SupportsGet = true)]
    public ApplicationStatus? Status { get; set; }

    /// <summary>Gets or sets optional environment filter.</summary>
    [BindProperty(SupportsGet = true)]
    public ApplicationEnvironment? Environment { get; set; }

    /// <summary>Gets or sets feedback message after operation.</summary>
    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>Gets or sets paginated applications result.</summary>
    public PagedResult<ApplicationListDto> Applications { get; set; } = new()
    {
        Items = [],
        PageIndex = 1,
        PageSize = 10,
        TotalCount = 0
    };

    /// <summary>Handles GET requests for listing applications.</summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var request = new PagedRequest
        {
            PageIndex = PageNumber,
            PageSize = PageSize,
            Search = SearchTerm,
            SortBy = "CreatedAt",
            SortDescending = true
        };
        var result = await applicationService.ListAsync(request, Status, Environment, tags: null, cancellationToken: cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Applications = result.Value;
        }
    }

    /// <summary>Handles POST requests for updating an application's client secret.</summary>
    public async Task<IActionResult> OnPostUpdateSecretAsync(string id, string? newSecret, CancellationToken cancellationToken)
    {
        var result = await applicationService.UpdateClientSecretAsync(id, newSecret, cancellationToken);
        if (result.IsSuccess)
        {
            StatusMessage = "Client secret successfully updated.";
        }
        else
        {
            StatusMessage = $"Failed to update client secret: {result.Error?.Description}";
        }
        return RedirectToPage();
    }

    /// <summary>Handles POST requests for deactivating an application (sets to Disabled).</summary>
    public async Task<IActionResult> OnPostDeactivateAsync(string id, CancellationToken cancellationToken)
    {
        var result = await applicationService.UpdateStatusAsync(id, ApplicationStatus.Disabled, cancellationToken);
        if (result.IsSuccess)
        {
            StatusMessage = "Application successfully deactivated.";
        }
        else
        {
            StatusMessage = $"Failed to deactivate: {result.Error?.Description}";
        }
        return RedirectToPage();
    }

    /// <summary>Handles POST requests for activating an application (sets to Active).</summary>
    public async Task<IActionResult> OnPostActivateAsync(string id, CancellationToken cancellationToken)
    {
        var result = await applicationService.UpdateStatusAsync(id, ApplicationStatus.Active, cancellationToken);
        if (result.IsSuccess)
        {
            StatusMessage = "Application successfully activated.";
        }
        else
        {
            StatusMessage = $"Failed to activate: {result.Error?.Description}";
        }
        return RedirectToPage();
    }

    /// <summary>Handles POST requests for deleting an application.</summary>
    public async Task<IActionResult> OnPostDeleteAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
    {
        var result = await applicationService.DeleteAsync(id, hardDelete, cancellationToken);
        if (result.IsSuccess)
        {
            StatusMessage = hardDelete ? "Application permanently deleted." : "Application status changed to deleted.";
        }
        else
        {
            StatusMessage = $"Delete failed: {result.Error?.Description}";
        }
        return RedirectToPage();
    }

    /// <summary>Handles POST requests for bulk updating applications status by environment.</summary>
    public async Task<IActionResult> OnPostBulkStatusAsync(
        string? bulkEnvironment,
        ApplicationStatus bulkStatus,
        CancellationToken cancellationToken)
    {
        ApplicationEnvironment? env = null;
        if (!string.IsNullOrWhiteSpace(bulkEnvironment) &&
            Enum.TryParse<ApplicationEnvironment>(bulkEnvironment, true, out var parsedEnv))
        {
            env = parsedEnv;
        }

        var result = await applicationService.SetStatusByEnvironmentAsync(env, bulkStatus, cancellationToken);
        if (result.IsSuccess)
        {
            var targetDesc = env.HasValue ? $"{env} environment" : "all environments";
            StatusMessage = $"Successfully set {result.Value} application(s) in {targetDesc} to {bulkStatus}.";
        }
        else
        {
            StatusMessage = $"Bulk update failed: {result.Error?.Description}";
        }

        return RedirectToPage();
    }
}
