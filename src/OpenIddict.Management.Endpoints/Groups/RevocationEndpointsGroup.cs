using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class RevocationEndpointsGroup
{
    public static RouteGroupBuilder MapRevocationEndpoints(this RouteGroupBuilder group)
    {
        var revocationGroup = group.MapGroup("/revocation")
            .WithTags("Revocation");

        revocationGroup.MapPost("/by-user", async (
            [FromBody] RevocationByUserRequest request,
            IOpenIddictRevocationManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.RevokeByUserAsync(request.UserId, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RevokeByUser")
        .WithSummary("Revoke tokens by user ID")
        .WithDescription("Revokes all active tokens and authorizations associated with a specific user identifier.")
        .Produces<RevocationResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        revocationGroup.MapPost("/by-client", async (
            [FromBody] RevocationByClientRequest request,
            IOpenIddictRevocationManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.RevokeByClientAsync(request.ClientId, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RevokeByClient")
        .WithSummary("Revoke tokens by client ID")
        .WithDescription("Revokes all active tokens and authorizations issued to a specific client application.")
        .Produces<RevocationResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        revocationGroup.MapPost("/by-session", async (
            [FromBody] RevocationBySessionRequest request,
            IOpenIddictRevocationManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.RevokeSessionAuthorizationsAsync(request.UserId, request.AuthorizationId, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RevokeBySession")
        .WithSummary("Revoke session authorizations")
        .WithDescription("Revokes authorizations and associated tokens for a specific user session or authorization ID.")
        .Produces<RevocationResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        revocationGroup.MapPost("/prune", async (
            IOpenIddictRevocationManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.PruneExpiredTokensAsync(cancellationToken);
            return result.ToHttpResult(count => HttpResults.Ok(new { PrunedTokensCount = count }));
        })
        .WithName("PruneExpiredTokens")
        .WithSummary("Prune expired and revoked tokens")
        .WithDescription("Removes expired and revoked tokens and authorizations from persistent storage to optimize database size.")
        .Produces(StatusCodes.Status200OK);

        return revocationGroup;
    }
}
