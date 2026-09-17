namespace OpenIddict.Management.Dto;

/// <summary>
/// Detailed data transfer object representing a session authorization and its associated tokens.
/// </summary>
public sealed record SessionDetailsDto
{
    /// <summary>
    /// Gets the unique identifier of the authorization.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the subject / user identifier associated with the session.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Gets the client identifier of the application.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Gets the display name of the application.
    /// </summary>
    public string? ClientDisplayName { get; init; }

    /// <summary>
    /// Gets the status of the authorization (e.g., valid, revoked).
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the type of authorization (e.g., permanent, ad-hoc).
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the scopes associated with the authorization.
    /// </summary>
    public string? Scopes { get; init; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the UTC last modified timestamp, if any.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; init; }

    /// <summary>
    /// Gets the list of tokens associated with this session authorization.
    /// </summary>
    public IReadOnlyList<TokenListDto> Tokens { get; init; } = [];
}
