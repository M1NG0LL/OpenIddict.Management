using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OpenIddict.Management.Endpoints.Groups;

internal static class CleanupEndpointsGroup
{
    public static RouteGroupBuilder MapCleanupEndpoints(this RouteGroupBuilder group)
    {
        var cleanupGroup = group.MapGroup("/cleanup")
            .WithTags("Token Cleanup");

        cleanupGroup.MapGet("/", ([FromServices] ITokenCleanupJobManager? jobManager) =>
        {
            if (jobManager is null)
            {
                return HttpResults.Problem(
                    detail: "Token cleanup job manager is not registered in the service container.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Cleanup Job Unavailable");
            }

            var status = new CleanupJobStatusDto
            {
                IsEnabled = jobManager.IsEnabled,
                BatchSize = jobManager.BatchSize,
                IntervalMinutes = (int)jobManager.Interval.TotalMinutes,
                IncludeRevoked = jobManager.IncludeRevoked,
                LastRunTime = jobManager.LastRunTime,
                LastPrunedCount = jobManager.LastPrunedCount,
                TotalPrunedCount = jobManager.TotalPrunedCount,
                LastStatus = jobManager.LastStatus,
                LastError = jobManager.LastError
            };

            return HttpResults.Ok(status);
        })
        .WithName("GetCleanupJobStatus")
        .WithSummary("Get token cleanup background job status")
        .WithDescription("Retrieves the current execution status, scheduling interval, batch size, and cumulative pruning metrics for the background token cleanup worker.")
        .Produces<CleanupJobStatusDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        cleanupGroup.MapPost("/toggle", (
            [FromQuery] bool? enabled,
            [FromServices] ITokenCleanupJobManager? jobManager) =>
        {
            if (jobManager is null)
            {
                return HttpResults.Problem(
                    detail: "Token cleanup job manager is not registered in the service container.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Cleanup Job Unavailable");
            }

            var newState = enabled ?? !jobManager.IsEnabled;
            jobManager.SetEnabled(newState);

            var status = new CleanupJobStatusDto
            {
                IsEnabled = jobManager.IsEnabled,
                BatchSize = jobManager.BatchSize,
                IntervalMinutes = (int)jobManager.Interval.TotalMinutes,
                IncludeRevoked = jobManager.IncludeRevoked,
                LastRunTime = jobManager.LastRunTime,
                LastPrunedCount = jobManager.LastPrunedCount,
                TotalPrunedCount = jobManager.TotalPrunedCount,
                LastStatus = jobManager.LastStatus,
                LastError = jobManager.LastError
            };

            return HttpResults.Ok(status);
        })
        .WithName("ToggleCleanupJob")
        .WithSummary("Toggle cleanup job activation state")
        .WithDescription("Enables, pauses, or toggles the active status of the background token cleanup job.")
        .Produces<CleanupJobStatusDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        cleanupGroup.MapPut("/settings", (
            [FromBody] UpdateCleanupJobSettingsRequest request,
            [FromServices] ITokenCleanupJobManager? jobManager) =>
        {
            if (jobManager is null)
            {
                return HttpResults.Problem(
                    detail: "Token cleanup job manager is not registered in the service container.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Cleanup Job Unavailable");
            }

            TimeSpan? interval = request.IntervalMinutes.HasValue && request.IntervalMinutes.Value > 0
                ? TimeSpan.FromMinutes(request.IntervalMinutes.Value)
                : null;

            jobManager.UpdateSettings(
                batchSize: request.BatchSize,
                interval: interval,
                includeRevoked: request.IncludeRevoked);

            var status = new CleanupJobStatusDto
            {
                IsEnabled = jobManager.IsEnabled,
                BatchSize = jobManager.BatchSize,
                IntervalMinutes = (int)jobManager.Interval.TotalMinutes,
                IncludeRevoked = jobManager.IncludeRevoked,
                LastRunTime = jobManager.LastRunTime,
                LastPrunedCount = jobManager.LastPrunedCount,
                TotalPrunedCount = jobManager.TotalPrunedCount,
                LastStatus = jobManager.LastStatus,
                LastError = jobManager.LastError
            };

            return HttpResults.Ok(status);
        })
        .WithName("UpdateCleanupJobSettings")
        .WithSummary("Update token cleanup job parameters")
        .WithDescription("Updates the batch size, interval schedule in minutes, and whether to include revoked tokens dynamically at runtime.")
        .Produces<CleanupJobStatusDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        cleanupGroup.MapPost("/run", async (
            [FromServices] ITokenCleanupJobManager? jobManager,
            CancellationToken cancellationToken) =>
        {
            if (jobManager is null)
            {
                return HttpResults.Problem(
                    detail: "Token cleanup job manager is not registered in the service container.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Cleanup Job Unavailable");
            }

            var prunedCount = await jobManager.TriggerRunAsync(cancellationToken);
            return HttpResults.Ok(new { PrunedTokensCount = prunedCount });
        })
        .WithName("RunCleanupJobNow")
        .WithSummary("Trigger immediate token cleanup run")
        .WithDescription("Forces an immediate, synchronous pass of the background cleanup job and updates execution metrics.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return cleanupGroup;
    }
}
