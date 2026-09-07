namespace OpenIddict.Management.Models;

/// <summary>
/// Represents a domain model for a managed OpenIddict authorization.
/// </summary>
public sealed record ManagedAuthorization
{
    /// <summary>
    /// Gets the unique identifier of the authorization.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the application identifier associated with this authorization.
    /// </summary>
    public required string ApplicationId { get; init; }

    /// <summary>
    /// Gets the subject (user ID) associated with this authorization.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Gets the status of the authorization (e.g. valid, revoked).
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the type of the authorization (e.g. permanent, ad-hoc).
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the scopes associated with the authorization.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Gets the UTC timestamp when the authorization was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the authorization was last modified.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; init; }
}
