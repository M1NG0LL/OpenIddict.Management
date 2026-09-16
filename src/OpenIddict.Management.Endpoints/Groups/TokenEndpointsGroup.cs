using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class TokenEndpointsGroup
{
    public static RouteGroupBuilder MapTokenEndpoints(this RouteGroupBuilder group)
    {
        var tokenGroup = group.MapGroup("/tokens")
            .WithTags("Tokens");

        tokenGroup.MapGet("/", async (
            [FromQuery(Name = "pageIndex")] int pageIndex = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 10,
            [FromQuery(Name = "search")] string? search = null,
            [FromQuery(Name = "userId")] string? userId = null,
            [FromQuery(Name = "clientId")] string? clientId = null,
            [FromQuery(Name = "authorizationId")] string? authorizationId = null,
            [FromQuery(Name = "status")] string? status = null,
            [FromQuery(Name = "tokenType")] string? tokenType = null,
            [FromQuery(Name = "createdFrom")] DateTimeOffset? createdFrom = null,
            [FromQuery(Name = "createdTo")] DateTimeOffset? createdTo = null,
            [FromServices] IOpenIddictTokenManager manager = null!,
            CancellationToken cancellationToken = default) =>
        {
            var filter = new TokenFilterRequest
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                SearchTerm = search,
                UserId = userId,
                ClientId = clientId,
                AuthorizationId = authorizationId,
                Status = status,
                TokenType = tokenType,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo
            };

            var result = await manager.ListTokensAsync(filter, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ListTokens")
        .WithSummary("List and filter tracked tokens")
        .WithDescription("Retrieves a paginated list of issued tokens with optional filtering by user, client, authorization, status, type, date range, or search keyword.")
        .Produces<PagedResult<TokenListDto>>(StatusCodes.Status200OK);

        tokenGroup.MapGet("/counts", async (
            [FromServices] IOpenIddictTokenManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.GetTokenCountsAsync(cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("GetTokenCounts")
        .WithSummary("Get aggregate token counts")
        .WithDescription("Retrieves summary counts of tokens categorized into total, valid, expired, and revoked.")
        .Produces<TokenCountSummaryDto>(StatusCodes.Status200OK);

        tokenGroup.MapPost("/revoke-filtered", async (
            [FromBody] TokenFilterRequest filter,
            [FromServices] IOpenIddictTokenManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.RevokeTokensWithFilterAsync(filter, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RevokeFilteredTokens")
        .WithSummary("Revoke tokens matching filter")
        .WithDescription("Revokes all active tokens and authorizations that match the specified filter criteria.")
        .Produces<RevocationResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        tokenGroup.MapGet("/timeline", async (
            [FromQuery(Name = "from")] DateOnly? from,
            [FromQuery(Name = "to")] DateOnly? to,
            [FromQuery(Name = "clientId")] string? clientId,
            [FromServices] IOpenIddictTokenManager manager,
            CancellationToken cancellationToken) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var fromDate = from ?? today.AddDays(-29);
            var toDate = to ?? today;

            var result = await manager.GetTokenTimelineAsync(fromDate, toDate, clientId, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("GetTokenTimeline")
        .WithSummary("Get token issuance timeline")
        .WithDescription("Retrieves daily token creation count data points across a date range, optionally filtered by client identifier.")
        .Produces<List<TokenTimelineDataPointDto>>(StatusCodes.Status200OK);

        tokenGroup.MapGet("/by-application", async Task<IResult> (
            [FromQuery(Name = "clientId")] string? clientId,
            [FromServices] IOpenIddictTokenManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.GetTokenCountsByApplicationAsync(cancellationToken);
            if (result.IsSuccess && result.Value is not null && !string.IsNullOrWhiteSpace(clientId))
            {
                var filtered = result.Value.Where(d => string.Equals(d.ClientId, clientId, StringComparison.OrdinalIgnoreCase)).ToList();
                return HttpResults.Ok(filtered);
            }
            return result.ToHttpResult();
        })
        .WithName("GetTokensByApplication")
        .WithSummary("Get token counts grouped by application")
        .WithDescription("Retrieves aggregate token counts grouped by registered client application for visualization and analysis.")
        .Produces<List<ApplicationTokenCountDto>>(StatusCodes.Status200OK);

        tokenGroup.MapDelete("/{id}", async (
            string id,
            [FromServices] IOpenIddictTokenManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.RevokeByTokenIdAsync(id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RevokeTokenById")
        .WithSummary("Revoke token by identifier")
        .WithDescription("Revokes a specific token by its unique token database identifier.")
        .Produces<RevocationResultDto>(StatusCodes.Status200OK);
        tokenGroup.MapPost("/extend", async (
            [FromBody] ExtendTokenExpirationRequest request,
            [FromServices] IOpenIddictTokenManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.ExtendTokenExpirationAsync(request.TokenIds, request.AdditionalMinutes, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ExtendTokenExpiration")
        .WithSummary("Extend expiration of tokens")
        .WithDescription("Extends the expiration time of specified tokens (valid or expired) by a given number of minutes.")
        .Produces<int>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        tokenGroup.MapPost("/revoke-multiple", async (
            [FromBody] RevokeMultipleTokensRequest request,
            [FromServices] IOpenIddictTokenManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.RevokeMultipleTokensAsync(request.TokenIds, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RevokeMultipleTokens")
        .WithSummary("Revoke multiple tokens by identifiers")
        .WithDescription("Revokes multiple active tokens in a single batch operation.")
        .Produces<RevocationResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        return tokenGroup;
    }
}
