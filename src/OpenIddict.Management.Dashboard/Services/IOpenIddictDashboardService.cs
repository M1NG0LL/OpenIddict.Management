namespace OpenIddict.Management.Dashboard.Services;

/// <summary>
/// Service contract for providing aggregated metrics and overview statistics for the admin dashboard.
/// </summary>
public interface IOpenIddictDashboardService
{
    /// <summary>
    /// Retrieves dashboard overview metrics including active applications, configured scopes, tokens, and active authorizations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DashboardOverviewDto"/> containing overview metrics.</returns>
    Task<DashboardOverviewDto> GetOverviewMetricsAsync(CancellationToken cancellationToken = default);
}
