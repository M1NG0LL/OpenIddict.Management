namespace OpenIddict.Management.Models;

/// <summary>
/// Represents a domain model for a managed OpenIddict token.
/// </summary>
public sealed record ManagedToken
{
    /// <summary>
    /// Gets the unique identifier of the token.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the application identifier associated with this token.
    /// </summary>
    public required string ApplicationId { get; init; }

    /// <summary>
    /// Gets the authorization identifier associated with this token.
    /// </summary>
    public string? AuthorizationId { get; init; }

    /// <summary>
    /// Gets the subject (user ID) associated with this token.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Gets the type of token (e.g. access_token, refresh_token).
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the status of the token (e.g. valid, revoked, inactive).
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the expiration date of the token.
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; init; }

    /// <summary>
    /// Gets the creation date of the token.
    /// </summary>
    public required DateTimeOffset CreationDate { get; init; }

    /// <summary>
    /// Gets the redemption date of the token.
    /// </summary>
    public DateTimeOffset? RedemptionDate { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the token was revoked, if applicable.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; init; }
}
