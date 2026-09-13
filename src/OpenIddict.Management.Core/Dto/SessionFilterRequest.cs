namespace OpenIddict.Management.Dto;

/// <summary>
/// Criteria for filtering, searching, and paginating authorizations (sessions).
/// </summary>
public sealed record SessionFilterRequest
{
    /// <summary>Gets or sets requested 1-indexed page index.</summary>
    public int PageIndex { get; init; } = 1;

    /// <summary>Gets or sets requested page size.</summary>
    public int PageSize { get; init; } = 10;

    /// <summary>Gets or sets optional search term across authorization ID, client ID, or user ID.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>Gets or sets user / subject filter.</summary>
    public string? UserId { get; init; }

    /// <summary>Gets or sets client ID filter.</summary>
    public string? ClientId { get; init; }

    /// <summary>Gets or sets authorization status filter (all, active/valid, revoked).</summary>
    public string? Status { get; init; }
}
