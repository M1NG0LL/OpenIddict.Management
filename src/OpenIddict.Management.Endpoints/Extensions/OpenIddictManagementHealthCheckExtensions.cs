using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenIddict.Management.Endpoints.Health;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering OpenIddict Management health checks.
/// </summary>
public static class OpenIddictManagementHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check for the OpenIddict background token cleanup job.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="name">Optional health check registration name. Defaults to "openiddict-token-cleanup".</param>
    /// <param name="failureStatus">Failure status to report when unhealthy.</param>
    /// <param name="tags">Optional tags for grouping health checks.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHealthChecksBuilder AddOpenIddictCleanupHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "openiddict-token-cleanup",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddCheck<TokenCleanupHealthCheck>(
            name,
            failureStatus,
            tags ?? ["openiddict", "cleanup"]);
    }
}
