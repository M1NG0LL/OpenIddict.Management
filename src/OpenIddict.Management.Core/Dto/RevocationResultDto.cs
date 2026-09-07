using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object representing the result of a token or authorization revocation operation.
/// </summary>
public sealed record RevocationResultDto
{
    /// <summary>
    /// Gets the number of tokens revoked.
    /// </summary>
    public required int TokensRevoked { get; init; }

    /// <summary>
    /// Gets the number of authorizations revoked.
    /// </summary>
    public int AuthorizationsRevoked { get; init; }

    /// <summary>
    /// Gets the scope of the revocation operation.
    /// </summary>
    public required RevocationScope Scope { get; init; }
}
