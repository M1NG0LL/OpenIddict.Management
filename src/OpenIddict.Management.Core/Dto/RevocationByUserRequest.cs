namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for revoking all tokens belonging to a specific user.
/// </summary>
public sealed record RevocationByUserRequest
{
    /// <summary>
    /// Gets the unique user identifier.
    /// </summary>
    public required string UserId { get; init; }
}
