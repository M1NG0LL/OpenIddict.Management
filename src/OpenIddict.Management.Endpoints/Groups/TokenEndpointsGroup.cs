using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class TokenEndpointsGroup
{
    public static RouteGroupBuilder MapTokenEndpoints(this RouteGroupBuilder group)
    {
        var tokenGroup = group.MapGroup("/tokens")
            .WithTags("Tokens");

        tokenGroup.MapDelete("/{id}", async (
            string id,
            IOpenIddictRevocationManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.RevokeByTokenIdAsync(id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RevokeTokenById")
        .WithSummary("Revoke token by identifier")
        .WithDescription("Revokes a specific token by its unique token database identifier.")
        .Produces<RevocationResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        return tokenGroup;
    }
}
