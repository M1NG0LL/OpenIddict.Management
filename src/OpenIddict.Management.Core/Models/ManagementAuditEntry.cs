namespace OpenIddict.Management.Models;

/// <summary>
/// Represents an audit trail log entry capturing a security or administrative operation in OpenIddict Management.
/// </summary>
public sealed record ManagementAuditEntry
{
    /// <summary>
    /// Gets the unique identifier of the audit record.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the category of the audited entity (e.g., "Application", "Scope", "Token", "Authorization", "Job").
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the action performed (e.g., "Created", "Updated", "Deleted", "Revoked", "Pruned", "RotatedSecret").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the unique identifier of the affected entity, if applicable.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Gets the display name or key of the affected entity, if applicable.
    /// </summary>
    public string? EntityName { get; init; }

    /// <summary>
    /// Gets the identity of the actor (user, client, or system) that initiated the operation.
    /// </summary>
    public string? Actor { get; init; }

    /// <summary>
    /// Gets additional structured or human-readable details about the operation.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
