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
            [FromQuery(Name = "sortBy")] string? sortBy = null,
            [FromQuery(Name = "sortDescending")] bool sortDescending = true,
            [FromServices] IOpenIddictAuthorizationManager manager = null!,
            CancellationToken cancellationToken = default) =>
        {
            var filter = new SessionFilterRequest
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                SearchTerm = search,
                UserId = userId,
                ClientId = clientId,
                Status = status,
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            var result = await manager.ListSessionsAsync(filter, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ListSessions")
        .WithSummary("List user session authorizations")
        .WithDescription("Retrieves a paginated list of authorizations and active user sessions with optional filtering by user, client, status, or search term.")
        .Produces<PagedResult<SessionListDto>>(StatusCodes.Status200OK);

        sessionGroup.MapGet("/{id}", async (
            string id,
            [FromServices] IOpenIddictAuthorizationManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.GetByIdAsync(id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("GetSessionById")
        .WithSummary("Get session authorization details")
        .WithDescription("Retrieves detailed session authorization information by identifier, including associated tokens.")
        .Produces<SessionDetailsDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        sessionGroup.MapDelete("/{id}", async (
            string id,
            [FromServices] IOpenIddictAuthorizationManager manager,
            CancellationToken cancellationToken) =>
        {
            var result = await manager.DeleteAsync(id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("DeleteSession")
        .WithSummary("Delete session authorization")
        .WithDescription("Permanently deletes a session authorization and all its associated tokens by unique identifier.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        return sessionGroup;
    }
}
