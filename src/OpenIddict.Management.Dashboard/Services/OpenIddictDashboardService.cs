using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;

namespace OpenIddict.Management.Dashboard.Services;

/// <summary>
/// Default implementation of <see cref="IOpenIddictDashboardService"/> for retrieving dashboard overview metrics.
/// </summary>
public class OpenIddictDashboardService(
    IApplicationManagementService? applicationService = null,
    IScopeManagementService? scopeService = null,
    IOpenIddictRevocationManager? revocationManager = null,
    IOpenIddictAuthorizationManager? authorizationManager = null,
    IOpenIddictTokenManager? tokenManager = null) : IOpenIddictDashboardService
{
    /// <inheritdoc/>
    public virtual async Task<DashboardOverviewDto> GetOverviewMetricsAsync(CancellationToken cancellationToken = default)
    {
        int totalApps = 0;
        int activeApps = 0;
        if (applicationService is not null)
        {
            var appsResult = await applicationService.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 1000 }, cancellationToken: cancellationToken);
            if (appsResult.IsSuccess && appsResult.Value is not null)
            {
                totalApps = appsResult.Value.TotalCount;
                activeApps = appsResult.Value.Items.Count(a => a.Status == Enums.ApplicationStatus.Active);
            }
        }

        int totalScopes = 0;
        if (scopeService is not null)
        {
            var scopesResult = await scopeService.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 1000 }, cancellationToken: cancellationToken);
            if (scopesResult.IsSuccess && scopesResult.Value is not null)
            {
                totalScopes = scopesResult.Value.TotalCount;
            }
        }

        int totalTokens = 0;
        int revokedTokens = 0;
        if (revocationManager is not null)
        {
            var tokenCountsResult = await revocationManager.GetTokenCountsAsync(cancellationToken);
            if (tokenCountsResult.IsSuccess && tokenCountsResult.Value is not null)
            {
                totalTokens = tokenCountsResult.Value.Total;
                revokedTokens = tokenCountsResult.Value.Revoked;
            }
        }
        else if (tokenManager is not null)
        {
            totalTokens = (int)await tokenManager.CountAsync(cancellationToken);
        }

        int activeAuthorizations = 0;
        if (revocationManager is not null)
        {
            var authResult = await revocationManager.GetActiveAuthorizationsCountAsync(cancellationToken);
            if (authResult.IsSuccess)
            {
                activeAuthorizations = authResult.Value;
            }
        }
        else if (authorizationManager is not null)
        {
            activeAuthorizations = (int)await authorizationManager.CountAsync(cancellationToken);
        }

        return new DashboardOverviewDto
        {
            ActiveApplications = activeApps,
            TotalApplications = totalApps,
            ConfiguredScopes = totalScopes,
            Tokens = totalTokens,
            RevokedTokens = revokedTokens,
            ActiveAuthorizations = activeAuthorizations
        };
    }
}
