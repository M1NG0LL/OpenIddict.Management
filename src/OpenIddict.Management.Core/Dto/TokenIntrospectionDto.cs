namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object representing the decoded introspection details of an OpenIddict token.
/// </summary>
public sealed record TokenIntrospectionDto
{
    /// <summary>
    /// Gets a value indicating whether the token is currently active (not revoked, not expired).
    /// </summary>
    public bool Active { get; init; }

    /// <summary>
    /// Gets the unique database identifier of the token.
    /// </summary>
    public required string TokenId { get; init; }

    /// <summary>
    /// Gets the optional reference identifier of the token.
    /// </summary>
    public string? ReferenceId { get; init; }

    /// <summary>
    /// Gets the type of token (e.g., access_token, refresh_token, authorization_code).
    /// </summary>
    public string? TokenType { get; init; }

    /// <summary>
    /// Gets the subject / user identifier of the token principal.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Gets the client identifier of the application associated with the token.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Gets the client display name.
    /// </summary>
    public string? ClientDisplayName { get; init; }

    /// <summary>
    /// Gets the UTC issuance date.
    /// </summary>
    public DateTimeOffset? IssuedAt { get; init; }

    /// <summary>
    /// Gets the UTC expiration date.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Gets the UTC revocation timestamp, if revoked.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; init; }

    /// <summary>
    /// Gets the status string (e.g., "valid", "revoked", "expired").
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the list of granted scopes parsed from the token.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Gets key-value claims extracted from token properties or payload.
    /// </summary>
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
}
