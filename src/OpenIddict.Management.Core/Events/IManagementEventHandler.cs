namespace OpenIddict.Management.Events;

/// <summary>
/// Defines a handler for OpenIddict Management domain events.
/// </summary>
/// <typeparam name="TEvent">The specific domain event type.</typeparam>
public interface IManagementEventHandler<in TEvent>
    where TEvent : IManagementEvent
{
    /// <summary>
    /// Handles the specified domain event asynchronously.
    /// </summary>
    /// <param name="event">The domain event instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the event processing.</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
