namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for revoking multiple tokens by their identifiers.
/// </summary>
public sealed record RevokeMultipleTokensRequest
{
    /// <summary>
    /// Gets the collection of token identifiers to revoke.
    /// </summary>
    public required IReadOnlyList<string> TokenIds { get; init; }
}
