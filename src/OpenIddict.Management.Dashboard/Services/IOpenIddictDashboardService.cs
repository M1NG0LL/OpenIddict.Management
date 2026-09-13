using OpenIddict.Management.Dto;

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

    /// <summary>
    /// Retrieves token counts grouped by application for chart visualizations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="ApplicationTokenCountDto"/>.</returns>
    Task<List<ApplicationTokenCountDto>> GetTokenCountsByApplicationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves daily token creation counts over a date range, optionally filtered by client identifier.
    /// </summary>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <param name="clientId">Optional client application identifier to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="TokenTimelineDataPointDto"/>.</returns>
    Task<List<TokenTimelineDataPointDto>> GetTokenTimelineAsync(
        DateOnly from,
        DateOnly to,
        string? clientId = null,
        CancellationToken cancellationToken = default);
}
