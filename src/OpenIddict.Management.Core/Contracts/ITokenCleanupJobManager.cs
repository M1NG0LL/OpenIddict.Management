using OpenIddict.Management.Options;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for managing the background token cleanup job state, configuration, and execution cycles.
/// </summary>
public interface ITokenCleanupJobManager
{
    /// <summary>
    /// Gets a value indicating whether the background cleanup job is currently enabled and executing.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the dynamic maximum amount of tokens cleared per cycle.
    /// </summary>
    int BatchSize { get; }

    /// <summary>
    /// Gets the execution interval of the background cleanup job.
    /// </summary>
    TimeSpan Interval { get; }

    /// <summary>
    /// Gets a value indicating whether revoked tokens are pruned along with expired tokens.
    /// </summary>
    bool IncludeRevoked { get; }

    /// <summary>
    /// Gets the timestamp of the last executed cleanup cycle in UTC, if any.
    /// </summary>
    DateTimeOffset? LastRunTime { get; }

    /// <summary>
    /// Gets the number of tokens pruned during the last cleanup cycle.
    /// </summary>
    int LastPrunedCount { get; }

    /// <summary>
    /// Gets the cumulative count of tokens pruned across all runs since application startup.
    /// </summary>
    int TotalPrunedCount { get; }

    /// <summary>
    /// Gets the current status label of the background cleanup job (e.g., "Idle", "Running", "Paused").
    /// </summary>
    string LastStatus { get; }

    /// <summary>
    /// Gets the error message from the last failed cleanup cycle, if any.
    /// </summary>
    string? LastError { get; }

    /// <summary>
    /// Sets whether the background job should actively run cleanup cycles.
    /// </summary>
    /// <param name="enabled">True to enable, false to pause.</param>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Sets the dynamic amount of tokens to clear per cleanup cycle.
    /// </summary>
    /// <param name="batchSize">Positive integer for batch size.</param>
    void SetBatchSize(int batchSize);

    /// <summary>
    /// Updates configuration settings dynamically at runtime.
    /// </summary>
    /// <param name="isEnabled">Optional enabled state.</param>
    /// <param name="batchSize">Optional batch size.</param>
    /// <param name="interval">Optional interval.</param>
    /// <param name="includeRevoked">Optional includeRevoked flag.</param>
    void UpdateSettings(bool? isEnabled = null, int? batchSize = null, TimeSpan? interval = null, bool? includeRevoked = null);

    /// <summary>
    /// Signals the background worker to immediately execute a cleanup cycle if enabled.
    /// </summary>
    void WakeUp();

    /// <summary>
    /// Triggers an immediate, synchronous cleanup run on-demand.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of pruned tokens.</returns>
    Task<int> TriggerRunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a single cycle of the cleanup job within the provided service scope context.
    /// Called by the background service.
    /// </summary>
    /// <param name="serviceProvider">Scoped service provider or root provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of pruned tokens in this cycle.</returns>
    Task<int> RunJobCycleAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for either the next scheduled run interval or an explicit wake-up signal.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the delay wait.</returns>
    Task WaitForNextExecutionAsync(CancellationToken cancellationToken);
}
