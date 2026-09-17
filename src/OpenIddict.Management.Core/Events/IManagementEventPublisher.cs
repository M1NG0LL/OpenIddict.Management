namespace OpenIddict.Management.Events;

/// <summary>
/// Service contract for publishing OpenIddict Management domain events to registered handlers.
/// </summary>
public interface IManagementEventPublisher
{
    /// <summary>
    /// Publishes a domain event asynchronously to all registered <see cref="IManagementEventHandler{TEvent}"/> implementations.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="event">The domain event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the dispatch.</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IManagementEvent;
}
