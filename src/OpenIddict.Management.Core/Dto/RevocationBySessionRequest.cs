namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for revoking session authorizations.
/// </summary>
public sealed record RevocationBySessionRequest
{
    /// <summary>
    /// Gets the unique user identifier associated with the session, if applicable.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the authorization identifier for the session, if applicable.
    /// </summary>
    public string? AuthorizationId { get; init; }
}
