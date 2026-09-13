using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class OverviewEndpointsGroup
{
    public static RouteGroupBuilder MapOverviewEndpoints(this RouteGroupBuilder group)
    {
        var overviewGroup = group.MapGroup("/overview")
            .WithTags("Overview");

        overviewGroup.MapGet("/", async (
            [FromServices] IApplicationManagementService appService,
            [FromServices] IScopeManagementService scopeService,
            [FromServices] IOpenIddictRevocationManager revocationManager,
            CancellationToken cancellationToken) =>
        {
            var totalApps = 0;
            var activeApps = 0;
            var appsResult = await appService.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 1000 }, cancellationToken: cancellationToken);
            if (appsResult.IsSuccess && appsResult.Value is not null)
            {
                totalApps = appsResult.Value.TotalCount;
                activeApps = appsResult.Value.Items.Count(a => a.Status == ApplicationStatus.Active);
            }

            var totalScopes = 0;
            var scopesResult = await scopeService.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 1000 }, cancellationToken: cancellationToken);
            if (scopesResult.IsSuccess && scopesResult.Value is not null)
            {
                totalScopes = scopesResult.Value.TotalCount;
            }

            var totalTokens = 0;
            var revokedTokens = 0;
            var tokenCountsResult = await revocationManager.GetTokenCountsAsync(cancellationToken);
            if (tokenCountsResult.IsSuccess && tokenCountsResult.Value is not null)
            {
                totalTokens = tokenCountsResult.Value.Total;
                revokedTokens = tokenCountsResult.Value.Revoked;
            }

            var activeAuthorizations = 0;
            var authResult = await revocationManager.GetActiveAuthorizationsCountAsync(cancellationToken);
            if (authResult.IsSuccess)
            {
                activeAuthorizations = authResult.Value;
            }

            var overview = new DashboardOverviewDto
            {
                ActiveApplications = activeApps,
                TotalApplications = totalApps,
                ConfiguredScopes = totalScopes,
                Tokens = totalTokens,
                RevokedTokens = revokedTokens,
                ActiveAuthorizations = activeAuthorizations
            };

            return HttpResults.Ok(overview);
        })
        .WithName("GetDashboardOverview")
        .WithSummary("Get system overview metrics")
        .WithDescription("Retrieves aggregated system metrics including total/active applications, configured scopes, tracked/revoked tokens, and active session authorizations.")
        .Produces<DashboardOverviewDto>(StatusCodes.Status200OK);

        return overviewGroup;
    }
}
