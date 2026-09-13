using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class SessionEndpointsGroup
{
    public static RouteGroupBuilder MapSessionEndpoints(this RouteGroupBuilder group)
    {
        var sessionGroup = group.MapGroup("/sessions")
            .WithTags("Sessions");

        sessionGroup.MapGet("/", async (
            [FromQuery(Name = "pageIndex")] int pageIndex = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 10,
            [FromQuery(Name = "search")] string? search = null,
            [FromQuery(Name = "userId")] string? userId = null,
            [FromQuery(Name = "clientId")] string? clientId = null,
            [FromQuery(Name = "status")] string? status = null,
            [FromServices] IOpenIddictRevocationManager manager = null!,
            CancellationToken cancellationToken = default) =>
        {
            var filter = new SessionFilterRequest
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                SearchTerm = search,
                UserId = userId,
                ClientId = clientId,
                Status = status
            };

            var result = await manager.ListSessionsAsync(filter, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ListSessions")
        .WithSummary("List user session authorizations")
        .WithDescription("Retrieves a paginated list of authorizations and active user sessions with optional filtering by user, client, status, or search term.")
        .Produces<PagedResult<SessionListDto>>(StatusCodes.Status200OK);

        return sessionGroup;
    }
}
