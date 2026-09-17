using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class AuditEndpointsGroup
{
    public static RouteGroupBuilder MapAuditEndpoints(this RouteGroupBuilder group)
    {
        var auditGroup = group.MapGroup("/audit")
            .WithTags("Audit");

        auditGroup.AddEndpointFilter(async (context, next) =>
        {
            var options = context.HttpContext.RequestServices.GetService<IOptions<OpenIddictManagementOptions>>()?.Value;
            if (options is not null && !options.EnableAuditLogging)
            {
                return HttpResults.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Audit logging disabled",
                    detail: "Audit trail logging is disabled in OpenIddict management configuration.");
            }

            return await next(context);
        });

        auditGroup.MapGet("/", async (
            [FromQuery(Name = "pageIndex")] int pageIndex = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 20,
            [FromQuery(Name = "search")] string? search = null,
            [FromQuery(Name = "category")] string? category = null,
            [FromQuery(Name = "action")] string? action = null,
            [FromQuery(Name = "entityId")] string? entityId = null,
            [FromQuery(Name = "actor")] string? actor = null,
            [FromQuery(Name = "success")] bool? success = null,
            [FromQuery(Name = "fromDate")] DateTimeOffset? fromDate = null,
            [FromQuery(Name = "toDate")] DateTimeOffset? toDate = null,
            [FromQuery(Name = "sortBy")] string? sortBy = null,
            [FromQuery(Name = "sortDescending")] bool sortDescending = true,
            [FromServices] IAuditTrailStore store = null!,
            CancellationToken cancellationToken = default) =>
        {
            var filter = new AuditFilterRequest
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Search = search,
                Category = category,
                Action = action,
                EntityId = entityId,
                Actor = actor,
                Success = success,
                FromDate = fromDate,
                ToDate = toDate,
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            var result = await store.QueryAsync(filter, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ListAuditEntries")
        .WithSummary("List audit trail entries")
        .WithDescription("Retrieves a paginated list of audit trail entries with optional filtering by date range, category, action, actor, success, or search terms.")
        .Produces<PagedResult<ManagementAuditEntry>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        auditGroup.MapGet("/{id}", async (
            string id,
            [FromServices] IAuditTrailStore store,
            CancellationToken cancellationToken) =>
        {
            var result = await store.GetByIdAsync(id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("GetAuditEntryById")
        .WithSummary("Get audit entry by identifier")
        .WithDescription("Retrieves a single audit trail entry by its unique identifier.")
        .Produces<ManagementAuditEntry>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);

        return auditGroup;
    }
}
