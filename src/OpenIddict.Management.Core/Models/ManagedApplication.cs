using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Models;

/// <summary>
/// Represents a domain model for a managed OpenIddict application.
/// </summary>
public sealed record ManagedApplication
{
    /// <summary>
    /// Gets the unique identifier of the application.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the client identifier of the application.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the display name of the application.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the operational status of the application.
    /// </summary>
    public ApplicationStatus Status { get; init; } = ApplicationStatus.Active;

    /// <summary>
    /// Gets the target environment of the application.
    /// </summary>
    public ApplicationEnvironment Environment { get; init; } = ApplicationEnvironment.Development;

    /// <summary>
    /// Gets the list of allowed roles for the application.
    /// </summary>
    public IReadOnlyList<string> AllowedRoles { get; init; } = [];

    /// <summary>
    /// Gets the description of the application.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the logo URI for the application.
    /// </summary>
    public string? LogoUri { get; init; }

    /// <summary>
    /// Gets the user ID of the application owner.
    /// </summary>
    public string? OwnerUserId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the application was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the application was last modified.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; init; }

    /// <summary>
    /// Gets the allowed redirect URIs.
    /// </summary>
    public IReadOnlyList<string> RedirectUris { get; init; } = [];

    /// <summary>
    /// Gets the allowed post-logout redirect URIs.
    /// </summary>
    public IReadOnlyList<string> PostLogoutRedirectUris { get; init; } = [];

    /// <summary>
    /// Gets the permissions granted to the application.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>
    /// Gets additional metadata for the application.
    /// </summary>
    public string? ExtraData { get; init; }

    /// <summary>
    /// Gets the tags associated with the application.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets the requirements enforced on the application.
    /// </summary>
    public IReadOnlyList<string> Requirements { get; init; } = [];

    /// <summary>
    /// Checks whether the application contains the specified tag.
    /// </summary>
    /// <param name="tag">The tag name to check.</param>
    /// <returns>True if the application contains the tag; otherwise, false.</returns>
    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || Tags is null or { Count: 0 })
        {
            return false;
        }

        return Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
    }
}
