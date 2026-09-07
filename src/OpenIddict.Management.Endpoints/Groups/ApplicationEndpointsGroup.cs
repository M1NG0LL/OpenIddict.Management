using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class ApplicationEndpointsGroup
{
    public static RouteGroupBuilder MapApplicationEndpoints(this RouteGroupBuilder group)
    {
        var appGroup = group.MapGroup("/applications")
            .WithTags("Applications");

        appGroup.MapGet("/", async (
            [FromQuery(Name = "pageIndex")] int pageIndex = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 20,
            [FromQuery(Name = "search")] string? search = null,
            [FromQuery(Name = "sortBy")] string? sortBy = null,
            [FromQuery(Name = "sortDescending")] bool sortDescending = false,
            [FromQuery(Name = "status")] ApplicationStatus? status = null,
            [FromQuery(Name = "environment")] ApplicationEnvironment? environment = null,
            [FromQuery(Name = "tags")] string[]? tags = null,
            IApplicationManagementService service = null!,
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

            var tagsList = tags is { Length: > 0 } ? tags.ToList() : null;
            var result = await service.ListAsync(request, status, environment, tagsList, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ListApplications")
        .WithSummary("List client applications")
        .WithDescription("Retrieves a paginated list of registered OpenID Connect / OAuth 2.0 client applications with optional filtering by status, environment, tags, and search keywords.")
        .Produces<PagedResult<ApplicationListDto>>(StatusCodes.Status200OK);

        appGroup.MapGet("/{id}", async (
            string id,
            IApplicationManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetByIdAsync(id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("GetApplicationById")
        .WithSummary("Get application by identifier")
        .WithDescription("Retrieves detailed configuration and metadata for a client application by its unique database identifier.")
        .Produces<ManagedApplication>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        appGroup.MapPost("/", async (
            [FromBody] ApplicationCreateDto dto,
            IApplicationManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(dto, cancellationToken);
            return result.ToHttpResult(app => HttpResults.Created($"/applications/{app.Id}", app));
        })
        .WithName("CreateApplication")
        .WithSummary("Create a new client application")
        .WithDescription("Registers a new OpenID Connect / OAuth 2.0 client application with client credentials, permissions, environment, tags, ExtraData, and redirect URIs.")
        .Produces<ManagedApplication>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        appGroup.MapPut("/{id}", async (
            string id,
            [FromBody] ApplicationUpdateDto dto,
            IApplicationManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateAsync(id, dto, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("UpdateApplication")
        .WithSummary("Update an existing application")
        .WithDescription("Updates general settings, status, environment, permissions, tags, ExtraData, and redirect URIs for an existing application.")
        .Produces<ManagedApplication>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        appGroup.MapDelete("/{id}", async (
            string id,
            [FromQuery] bool hard = false,
            IApplicationManagementService service = null!,
            CancellationToken cancellationToken = default) =>
        {
            var result = await service.DeleteAsync(id, hard, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("DeleteApplication")
        .WithSummary("Delete an application")
        .WithDescription("Deletes an application (sets status to Deleted by default, or performs permanent hard deletion when hard=true).")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        appGroup.MapPatch("/{id}/status", async (
            string id,
            [FromQuery] ApplicationStatus status,
            IApplicationManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateStatusAsync(id, status, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("UpdateApplicationStatus")
        .WithSummary("Update application operational status")
        .WithDescription("Updates the operational status (Active, Disabled, Deleted) for the specified application.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        appGroup.MapPost("/{id}/secret", async (
            string id,
            [FromBody] UpdateClientSecretRequest request,
            IApplicationManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateClientSecretAsync(id, request.ClientSecret, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("UpdateClientSecret")
        .WithSummary("Update application client secret")
        .WithDescription("Updates the client secret for the specified application.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        return appGroup;
    }
}
