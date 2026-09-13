namespace OpenIddict.Management.Dto;

/// <summary>
/// Represents daily aggregated token creation count for timeline analysis.
/// </summary>
public sealed record TokenTimelineDataPointDto
{
    /// <summary>Gets the date of token creation.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Gets the number of tokens created on this date.</summary>
    public int Count { get; init; }
}
