namespace OpenIddict.Management.Dto;

/// <summary>
/// Represents aggregated token count statistics for an application.
/// </summary>
public sealed record ApplicationTokenCountDto
{
    /// <summary>Gets the unique application identifier.</summary>
    public required string ApplicationId { get; init; }

    /// <summary>Gets the OAuth2 client identifier.</summary>
    public required string ClientId { get; init; }

    /// <summary>Gets the application display name, if available.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the total count of tokens associated with this application.</summary>
    public int TokenCount { get; init; }
}
