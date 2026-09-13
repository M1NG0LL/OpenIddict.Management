namespace OpenIddict.Management.Dto;

/// <summary>
/// Status and execution telemetry metrics for the background token cleanup job.
/// </summary>
public sealed record CleanupJobStatusDto
{
    /// <summary>Gets a value indicating whether the background cleanup job is active.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>Gets the amount of tokens pruned per cleanup run.</summary>
    public int BatchSize { get; init; }

    /// <summary>Gets the configured execution frequency in minutes.</summary>
    public int IntervalMinutes { get; init; }

    /// <summary>Gets a value indicating whether revoked tokens are included in pruning.</summary>
    public bool IncludeRevoked { get; init; }

    /// <summary>Gets the UTC timestamp of the last executed cleanup pass, if any.</summary>
    public DateTimeOffset? LastRunTime { get; init; }

    /// <summary>Gets the count of tokens pruned on the last cleanup pass.</summary>
    public int LastPrunedCount { get; init; }

    /// <summary>Gets the cumulative count of tokens pruned across all runs since application start.</summary>
    public int TotalPrunedCount { get; init; }

    /// <summary>Gets the current operational status label (e.g., "Idle", "Running", "Paused").</summary>
    public string LastStatus { get; init; } = "Idle";

    /// <summary>Gets the last recorded error message, if any.</summary>
    public string? LastError { get; init; }
}
