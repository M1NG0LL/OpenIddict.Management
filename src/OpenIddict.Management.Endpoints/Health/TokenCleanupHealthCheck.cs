using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenIddict.Management.Contracts;

namespace OpenIddict.Management.Endpoints.Health;

/// <summary>
/// Health check that reports the operational status of the OpenIddict background token cleanup job.
/// </summary>
public sealed class TokenCleanupHealthCheck : IHealthCheck
{
    private readonly ITokenCleanupJobManager _jobManager;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenCleanupHealthCheck"/>.
    /// </summary>
    /// <param name="jobManager">The token cleanup job manager.</param>
    public TokenCleanupHealthCheck(ITokenCleanupJobManager jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
    }

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["isEnabled"] = _jobManager.IsEnabled,
            ["status"] = _jobManager.LastStatus,
            ["lastRunTime"] = _jobManager.LastRunTime?.ToString("o") ?? "Never",
            ["lastPrunedCount"] = _jobManager.LastPrunedCount,
            ["totalPrunedCount"] = _jobManager.TotalPrunedCount
        };

        if (!string.IsNullOrEmpty(_jobManager.LastError))
        {
            data["lastError"] = _jobManager.LastError;
            return Task.FromResult(HealthCheckResult.Degraded(
                description: $"Token cleanup job encountered an error: {_jobManager.LastError}",
                data: data));
        }

        if (!_jobManager.IsEnabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                description: "Token cleanup job is currently paused or disabled.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            description: $"Token cleanup job is healthy (status: {_jobManager.LastStatus}).",
            data: data));
    }
}
