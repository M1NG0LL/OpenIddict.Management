using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Options;

namespace OpenIddict.Management.Services;

/// <summary>
/// Thread-safe singleton managing configuration, execution cycles, and status of the token cleanup job.
/// </summary>
public class TokenCleanupJobManager : ITokenCleanupJobManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenCleanupJobManager>? _logger;
    private readonly SemaphoreSlim _wakeUpSignal = new(0, 1);
    private readonly object _syncLock = new();

    private bool _isEnabled;
    private int _batchSize;
    private TimeSpan _interval;
    private bool _includeRevoked;
    private DateTimeOffset? _lastRunTime;
    private int _lastPrunedCount;
    private int _totalPrunedCount;
    private string _lastStatus = "Idle";
    private string? _lastError;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenCleanupJobManager"/>.
    /// </summary>
    public TokenCleanupJobManager(
        IServiceScopeFactory scopeFactory,
        IOptions<TokenCleanupOptions>? options = null,
        ILogger<TokenCleanupJobManager>? logger = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger;

        var opt = options?.Value ?? new TokenCleanupOptions();
        _isEnabled = opt.IsEnabled;
        _batchSize = opt.BatchSize > 0 ? opt.BatchSize : 100;
        _interval = opt.Interval > TimeSpan.Zero ? opt.Interval : TimeSpan.FromHours(1);
        _includeRevoked = opt.IncludeRevoked;
        _lastStatus = _isEnabled ? "Idle" : "Paused";
    }

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get { lock (_syncLock) return _isEnabled; }
    }

    /// <inheritdoc/>
    public int BatchSize
    {
        get { lock (_syncLock) return _batchSize; }
    }

    /// <inheritdoc/>
    public TimeSpan Interval
    {
        get { lock (_syncLock) return _interval; }
    }

    /// <inheritdoc/>
    public bool IncludeRevoked
    {
        get { lock (_syncLock) return _includeRevoked; }
    }

    /// <inheritdoc/>
    public DateTimeOffset? LastRunTime
    {
        get { lock (_syncLock) return _lastRunTime; }
    }

    /// <inheritdoc/>
    public int LastPrunedCount
    {
        get { lock (_syncLock) return _lastPrunedCount; }
    }

    /// <inheritdoc/>
    public int TotalPrunedCount
    {
        get { lock (_syncLock) return _totalPrunedCount; }
    }

    /// <inheritdoc/>
    public string LastStatus
    {
        get { lock (_syncLock) return _lastStatus; }
    }

    /// <inheritdoc/>
    public string? LastError
    {
        get { lock (_syncLock) return _lastError; }
    }

    /// <inheritdoc/>
    public void SetEnabled(bool enabled)
    {
        lock (_syncLock)
        {
            _isEnabled = enabled;
            _lastStatus = enabled ? "Idle" : "Paused";
        }

        _logger?.LogInformation("Token cleanup background job enabled state changed to: {Enabled}", enabled);

        if (enabled)
        {
            WakeUp();
        }
    }

    /// <inheritdoc/>
    public void SetBatchSize(int batchSize)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
        }

        lock (_syncLock)
        {
            _batchSize = batchSize;
        }

        _logger?.LogInformation("Token cleanup batch size updated to: {BatchSize}", batchSize);
    }

    /// <inheritdoc/>
    public void UpdateSettings(bool? isEnabled = null, int? batchSize = null, TimeSpan? interval = null, bool? includeRevoked = null)
    {
        bool wakeUpNeeded = false;

        lock (_syncLock)
        {
            if (isEnabled.HasValue)
            {
                if (!_isEnabled && isEnabled.Value)
                {
                    wakeUpNeeded = true;
                }
                _isEnabled = isEnabled.Value;
                _lastStatus = _isEnabled ? "Idle" : "Paused";
            }

            if (batchSize.HasValue && batchSize.Value > 0)
            {
                _batchSize = batchSize.Value;
            }

            if (interval.HasValue && interval.Value > TimeSpan.Zero)
            {
                _interval = interval.Value;
            }

            if (includeRevoked.HasValue)
            {
                _includeRevoked = includeRevoked.Value;
            }
        }

        _logger?.LogInformation("Token cleanup settings updated. Enabled: {Enabled}, BatchSize: {BatchSize}, Interval: {Interval}, IncludeRevoked: {IncludeRevoked}",
            _isEnabled, _batchSize, _interval, _includeRevoked);

        if (wakeUpNeeded)
        {
            WakeUp();
        }
    }

    /// <inheritdoc/>
    public void WakeUp()
    {
        try
        {
            if (_wakeUpSignal.CurrentCount == 0)
            {
                _wakeUpSignal.Release();
            }
        }
        catch (SemaphoreFullException)
        {
            // Already signaled
        }
    }

    /// <inheritdoc/>
    public async Task<int> TriggerRunAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        return await RunJobCycleAsync(scope.ServiceProvider, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> RunJobCycleAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        int currentBatchSize;
        bool currentIncludeRevoked;

        lock (_syncLock)
        {
            _lastStatus = "Running";
            currentBatchSize = _batchSize;
            currentIncludeRevoked = _includeRevoked;
        }

        try
        {
            using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var tokenManager = scope.ServiceProvider.GetService<IOpenIddictTokenManager>();

            if (tokenManager is null)
            {
                lock (_syncLock)
                {
                    _lastStatus = "NoStore";
                    _lastError = "IOpenIddictTokenManager is not registered in the service provider.";
                }
                _logger?.LogWarning("Token cleanup skipped: IOpenIddictTokenManager not registered.");
                return 0;
            }

            var result = await tokenManager.PruneTokensAsync(
                batchSize: currentBatchSize,
                includeRevoked: currentIncludeRevoked,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                var pruned = result.Value;
                lock (_syncLock)
                {
                    _lastRunTime = DateTimeOffset.UtcNow;
                    _lastPrunedCount = pruned;
                    _totalPrunedCount += pruned;
                    _lastError = null;
                    _lastStatus = _isEnabled ? "Idle" : "Paused";
                }

                _logger?.LogInformation("Token cleanup cycle completed: {PrunedCount} token(s) pruned.", pruned);
                return pruned;
            }
            else
            {
                var errorDesc = result.Error?.Description ?? "Unknown revocation error.";
                lock (_syncLock)
                {
                    _lastRunTime = DateTimeOffset.UtcNow;
                    _lastError = errorDesc;
                    _lastStatus = "Failed";
                }

                _logger?.LogError("Token cleanup cycle failed: {Error}", errorDesc);
                return 0;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            lock (_syncLock)
            {
                _lastRunTime = DateTimeOffset.UtcNow;
                _lastError = ex.Message;
                _lastStatus = "Failed";
            }

            _logger?.LogError(ex, "Unexpected error during token cleanup cycle.");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task WaitForNextExecutionAsync(CancellationToken cancellationToken)
    {
        TimeSpan currentInterval;
        lock (_syncLock)
        {
            currentInterval = _interval;
        }

        try
        {
            await _wakeUpSignal.WaitAsync(currentInterval, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down
        }
    }
}
