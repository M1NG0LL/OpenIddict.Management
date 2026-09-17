using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;

namespace OpenIddict.Management.Dashboard.Pages.Audit;

/// <summary>
/// Page model for Audit Trail listing and filtering.
/// </summary>
public class IndexModel(
    IAuditTrailStore auditTrailStore,
    IOptions<OpenIddictManagementOptions> managementOptions) : PageModel
{
    /// <summary>Gets a value indicating whether audit logging is enabled.</summary>
    public bool IsAuditLoggingEnabled => managementOptions.Value.EnableAuditLogging;

    /// <summary>Gets or sets the current page number.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Gets or sets the page size.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    /// <summary>Gets or sets the search term.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>Gets or sets the category filter.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    /// <summary>Gets or sets the action filter.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ActionFilter { get; set; }

    /// <summary>Gets or sets the success filter.</summary>
    [BindProperty(SupportsGet = true)]
    public bool? SuccessOnly { get; set; }

    /// <summary>Gets or sets the paged audit entries result.</summary>
    public PagedResult<ManagementAuditEntry> Entries { get; set; } = new()
    {
        Items = [],
        PageIndex = 1,
        PageSize = 20,
        TotalCount = 0
    };

    /// <summary>Handles GET request for listing audit entries.</summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsAuditLoggingEnabled)
        {
            return;
        }

        var filter = new AuditFilterRequest
        {
            PageIndex = PageNumber,
            PageSize = PageSize,
            Search = SearchTerm,
            Category = string.IsNullOrWhiteSpace(Category) ? null : Category,
            Action = string.IsNullOrWhiteSpace(ActionFilter) ? null : ActionFilter,
            Success = SuccessOnly,
            SortBy = "Timestamp",
            SortDescending = true
        };

        var result = await auditTrailStore.QueryAsync(filter, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Entries = result.Value;
        }
    }
}
