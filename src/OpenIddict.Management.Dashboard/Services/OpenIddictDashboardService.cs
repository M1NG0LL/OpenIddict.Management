using Abstractions = OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;

namespace OpenIddict.Management.Dashboard.Services;

/// <summary>
/// Default implementation of <see cref="IOpenIddictDashboardService"/> for retrieving dashboard overview metrics.
/// </summary>
public class OpenIddictDashboardService(
    IApplicationManagementService? applicationService = null,
    IScopeManagementService? scopeService = null,
    IOpenIddictTokenManager? tokenManager = null,
    IOpenIddictAuthorizationManager? authorizationManager = null,
    Abstractions.IOpenIddictAuthorizationManager? coreAuthorizationManager = null,
    Abstractions.IOpenIddictTokenManager? coreTokenManager = null) : IOpenIddictDashboardService
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
        if (tokenManager is not null)
        {
            var tokenCountsResult = await tokenManager.GetTokenCountsAsync(cancellationToken);
            if (tokenCountsResult.IsSuccess && tokenCountsResult.Value is not null)
            {
                totalTokens = tokenCountsResult.Value.Total;
                revokedTokens = tokenCountsResult.Value.Revoked;
            }
        }
        else if (coreTokenManager is not null)
        {
            totalTokens = (int)await coreTokenManager.CountAsync(cancellationToken);
        }

        int activeAuthorizations = 0;
        if (authorizationManager is not null)
        {
            var authResult = await authorizationManager.GetActiveAuthorizationsCountAsync(cancellationToken);
            if (authResult.IsSuccess)
            {
                activeAuthorizations = authResult.Value;
            }
        }
        else if (coreAuthorizationManager is not null)
        {
            activeAuthorizations = (int)await coreAuthorizationManager.CountAsync(cancellationToken);
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

    /// <inheritdoc/>
    public virtual async Task<List<ApplicationTokenCountDto>> GetTokenCountsByApplicationAsync(CancellationToken ct = default)
    {
        if (tokenManager is null)
        {
            return [];
        }

        var result = await tokenManager.GetTokenCountsByApplicationAsync(ct);
        return result.IsSuccess && result.Value is not null ? result.Value : [];
    }

    /// <inheritdoc/>
    public virtual async Task<List<TokenTimelineDataPointDto>> GetTokenTimelineAsync(
        DateOnly from,
        DateOnly to,
        string? clientId = null,
        CancellationToken ct = default)
    {
        if (tokenManager is null)
        {
            return [];
        }

        var result = await tokenManager.GetTokenTimelineAsync(from, to, clientId, ct);
        return result.IsSuccess && result.Value is not null ? result.Value : [];
    }
}
