using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Management.Contracts;

namespace OpenIddict.Management.Services;

/// <summary>
/// Hosted background service that periodically clears expired and revoked tokens from storage.
/// </summary>
public class TokenCleanupBackgroundService : BackgroundService
{
    private readonly ITokenCleanupJobManager _jobManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupBackgroundService>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenCleanupBackgroundService"/>.
    /// </summary>
    public TokenCleanupBackgroundService(
        ITokenCleanupJobManager jobManager,
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupBackgroundService>? logger = null)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation("Token cleanup background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_jobManager.IsEnabled)
                {
                    _logger?.LogDebug("Starting scheduled token cleanup cycle with batch limit: {BatchSize}", _jobManager.BatchSize);
                    await _jobManager.RunJobCycleAsync(_serviceProvider, stoppingToken);
                }
                else
                {
                    _logger?.LogDebug("Token cleanup background service is paused. Skipping execution cycle.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogError(ex, "Unhandled exception during token cleanup cycle.");
            }

            await _jobManager.WaitForNextExecutionAsync(stoppingToken);
        }

        _logger?.LogInformation("Token cleanup background service stopped.");
    }
}
