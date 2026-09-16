namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for extending the expiration time of one or more tokens.
/// </summary>
public sealed record ExtendTokenExpirationRequest
{
    /// <summary>
    /// Gets the collection of token identifiers to extend.
    /// </summary>
    public required IReadOnlyList<string> TokenIds { get; init; }

    /// <summary>
    /// Gets the additional minutes to add to token expiration.
    /// </summary>
    public required int AdditionalMinutes { get; init; }
}
