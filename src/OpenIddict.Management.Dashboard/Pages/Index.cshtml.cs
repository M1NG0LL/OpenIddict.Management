using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Management.Dashboard.Services;

namespace OpenIddict.Management.Dashboard.Pages;

/// <summary>
/// Page model for the admin dashboard overview homepage.
/// </summary>
public class IndexModel(IOpenIddictDashboardService dashboardService) : PageModel
{
    /// <summary>
    /// Gets the retrieved dashboard overview metrics.
    /// </summary>
    public DashboardOverviewDto Overview { get; private set; } = new();

    /// <summary>
    /// Gets total registered applications count.
    /// </summary>
    public int TotalApplications => Overview.TotalApplications;

    /// <summary>
    /// Gets active applications count.
    /// </summary>
    public int ActiveApplications => Overview.ActiveApplications;

    /// <summary>
    /// Gets total configured scopes count.
    /// </summary>
    public int TotalScopes => Overview.ConfiguredScopes;

    /// <summary>
    /// Gets total tracked tokens count.
    /// </summary>
    public int TotalTokens => Overview.Tokens;

    /// <summary>
    /// Gets revoked tokens count.
    /// </summary>
    public int RevokedTokens => Overview.RevokedTokens;

    /// <summary>
    /// Gets active authorizations count.
    /// </summary>
    public int ActiveAuthorizations => Overview.ActiveAuthorizations;

    /// <summary>
    /// Gets active authorizations count for backwards compatibility.
    /// </summary>
    public int ActiveSessions => Overview.ActiveAuthorizations;

    /// <summary>
    /// Handles GET request for the dashboard overview page.
    /// </summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Overview = await dashboardService.GetOverviewMetricsAsync(cancellationToken);
    }
}
