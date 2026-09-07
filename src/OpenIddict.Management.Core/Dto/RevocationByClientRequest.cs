namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for revoking all tokens belonging to a specific client application.
/// </summary>
public sealed record RevocationByClientRequest
{
    /// <summary>
    /// Gets the unique client identifier or application ID.
    /// </summary>
    public required string ClientId { get; init; }
}
