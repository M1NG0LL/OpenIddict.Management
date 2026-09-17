namespace OpenIddict.Management.Events;

/// <summary>
/// Marker interface representing a domain event published within the OpenIddict Management ecosystem.
/// </summary>
public interface IManagementEvent
{
    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the category of the affected domain area (e.g., "Application", "Scope", "Token", "Authorization", "Job").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the action name (e.g., "Created", "Updated", "Deleted", "Revoked", "Pruned").
    /// </summary>
    string Action { get; }

    /// <summary>
    /// Gets the optional actor identity who triggered the event.
    /// </summary>
    string? Actor { get; }
}
