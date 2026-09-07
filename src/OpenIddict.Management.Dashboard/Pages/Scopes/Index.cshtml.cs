using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;

namespace OpenIddict.Management.Dashboard.Pages.Scopes;

/// <summary>
/// Page model for Scope Manager listing.
/// </summary>
public class IndexModel(IScopeManagementService scopeService) : PageModel
{
    /// <summary>Gets or sets page index.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Gets or sets page size.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    /// <summary>Gets or sets optional search term.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>Gets or sets paginated scopes result.</summary>
    public PagedResult<ManagedScope> Scopes { get; set; } = new()
    {
        Items = [],
        PageIndex = 1,
        PageSize = 10,
        TotalCount = 0
    };

    /// <summary>Handles GET request for listing scopes.</summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var request = new PagedRequest
        {
            PageIndex = PageNumber,
            PageSize = PageSize,
            Search = SearchTerm
        };
        var result = await scopeService.ListAsync(request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Scopes = result.Value;
        }
    }

    /// <summary>Handles POST request for deleting a scope.</summary>
    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken cancellationToken)
    {
        await scopeService.DeleteAsync(id, cancellationToken);
        return RedirectToPage();
    }
}
