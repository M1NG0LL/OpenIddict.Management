using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Options;

namespace OpenIddict.Management.Events;

/// <summary>
/// Default implementation of <see cref="IManagementEventPublisher"/> that dispatches events to all registered handlers
/// using the service provider with fault isolation and logging.
/// </summary>
public sealed class ManagementEventPublisher(
    IServiceProvider serviceProvider,
    IOptions<OpenIddictManagementOptions> options,
    ILogger<ManagementEventPublisher> logger) : IManagementEventPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IManagementEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (!options.Value.EnableAuditLogging)
        {
            return;
        }

        try
        {
            var handlers = serviceProvider.GetServices<IManagementEventHandler<TEvent>>();
            foreach (var handler in handlers)
            {
                try
                {
                    await handler.HandleAsync(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while handling event {EventType} by handler {HandlerType}",
                        typeof(TEvent).Name, handler.GetType().Name);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve or execute event handlers for {EventType}", typeof(TEvent).Name);
        }
    }
}
