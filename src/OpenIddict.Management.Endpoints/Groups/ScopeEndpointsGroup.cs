using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Models;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class ScopeEndpointsGroup
{
    public static RouteGroupBuilder MapScopeEndpoints(this RouteGroupBuilder group)
    {
        var scopeGroup = group.MapGroup("/scopes")
            .WithTags("Scopes");

        scopeGroup.MapGet("/", async (
            [FromQuery(Name = "pageIndex")] int pageIndex = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 20,
            [FromQuery(Name = "search")] string? search = null,
            [FromQuery(Name = "sortBy")] string? sortBy = null,
            [FromQuery(Name = "sortDescending")] bool sortDescending = false,
            IScopeManagementService service = null!,
            CancellationToken cancellationToken = default) =>
        {
            var request = new PagedRequest
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Search = search,
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            var result = await service.ListAsync(request, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ListScopes")
        .WithSummary("List scopes")
        .WithDescription("Retrieves a paginated list of registered OpenID Connect / OAuth 2.0 scopes with optional search filtering.")
        .Produces<PagedResult<ManagedScope>>(StatusCodes.Status200OK);

        scopeGroup.MapGet("/{id}", async (
            string id,
            IScopeManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetByIdAsync(id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("GetScopeById")
        .WithSummary("Get scope by identifier")
        .WithDescription("Retrieves details of an OpenID Connect scope by its unique database identifier.")
        .Produces<ManagedScope>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        scopeGroup.MapGet("/by-name/{name}", async (
            string name,
            IScopeManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetByNameAsync(name, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("GetScopeByName")
        .WithSummary("Get scope by name")
        .WithDescription("Retrieves details of an OpenID Connect scope by its unique scope name.")
        .Produces<ManagedScope>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        scopeGroup.MapPost("/", async (
            [FromBody] CreateScopeRequest request,
            IScopeManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(request.Name, request.DisplayName, request.Description, request.Resources, cancellationToken);
            return result.ToHttpResult(scope => HttpResults.Created($"/scopes/{scope.Id}", scope));
        })
        .WithName("CreateScope")
        .WithSummary("Create a new scope")
        .WithDescription("Registers a new OpenID Connect scope with a unique name, display name, description, and associated resource identifiers.")
        .Produces<ManagedScope>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        scopeGroup.MapPut("/{id}", async (
            string id,
            [FromBody] UpdateScopeRequest request,
            IScopeManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateAsync(id, request.DisplayName, request.Description, request.Resources, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("UpdateScope")
        .WithSummary("Update an existing scope")
        .WithDescription("Updates the display name, description, and associated resources for an existing scope.")
        .Produces<ManagedScope>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        scopeGroup.MapDelete("/{id}", async (
            string id,
            IScopeManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteAsync(id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("DeleteScope")
        .WithSummary("Delete a scope")
        .WithDescription("Deletes an OpenID Connect scope by its unique database identifier.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        return scopeGroup;
    }
}
