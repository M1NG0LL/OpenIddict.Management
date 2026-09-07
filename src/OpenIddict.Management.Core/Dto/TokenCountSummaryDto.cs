namespace OpenIddict.Management.Dto;

/// <summary>
/// Represents aggregate counts of tokens categorized by status.
/// </summary>
public sealed record TokenCountSummaryDto
{
    /// <summary>
    /// Gets the total count of all tracked tokens.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Gets the count of active, non-expired, non-revoked tokens.
    /// </summary>
    public int Valid { get; init; }

    /// <summary>
    /// Gets the count of expired tokens.
    /// </summary>
    public int Expired { get; init; }

    /// <summary>
    /// Gets the count of revoked tokens.
    /// </summary>
    public int Revoked { get; init; }
}
