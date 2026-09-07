namespace OpenIddict.Management.Models;

/// <summary>
/// Represents a domain model for a managed OpenIddict scope.
/// </summary>
public sealed record ManagedScope
{
    /// <summary>
    /// Gets the unique identifier of the scope.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the name of the scope.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the display name of the scope.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the description of the scope.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the resources associated with the scope.
    /// </summary>
    public IReadOnlyList<string> Resources { get; init; } = [];

    /// <summary>
    /// Gets the UTC timestamp when the scope was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the scope was last modified.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; init; }
}
