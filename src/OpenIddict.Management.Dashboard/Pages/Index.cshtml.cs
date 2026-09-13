using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Management.Dashboard.Services;
using OpenIddict.Management.Dto;

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
    /// Gets the aggregated token counts per application for charts.
    /// </summary>
    public List<ApplicationTokenCountDto> AppTokenCounts { get; private set; } = [];

    /// <summary>
    /// Gets the daily token creation timeline data points for charts.
    /// </summary>
    public List<TokenTimelineDataPointDto> TokenTimeline { get; private set; } = [];

    /// <summary>
    /// Handles GET request for the dashboard overview page.
    /// </summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Overview = await dashboardService.GetOverviewMetricsAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = today.AddDays(-29);

        AppTokenCounts = await dashboardService.GetTokenCountsByApplicationAsync(cancellationToken);
        TokenTimeline = await dashboardService.GetTokenTimelineAsync(fromDate, today, null, cancellationToken);
    }

    /// <summary>
    /// Handles AJAX request for fetching token timeline data points.
    /// </summary>
    public async Task<IActionResult> OnGetTokenTimelineAsync(string? clientId = null, int days = 30, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = today.AddDays(-Math.Max(1, days - 1));
        var data = await dashboardService.GetTokenTimelineAsync(fromDate, today, clientId, cancellationToken);
        return new JsonResult(data);
    }

    /// <summary>
    /// Handles AJAX request for fetching per-application token counts.
    /// </summary>
    public async Task<IActionResult> OnGetTokensByAppAsync(string? clientId = null, CancellationToken cancellationToken = default)
    {
        var data = await dashboardService.GetTokenCountsByApplicationAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            data = data.Where(d => string.Equals(d.ClientId, clientId, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return new JsonResult(data);
    }
}
