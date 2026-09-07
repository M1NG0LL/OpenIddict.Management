namespace OpenIddict.Management.Dto;

/// <summary>
/// Criteria for filtering, searching, and paginating tokens in Token Inspector.
/// </summary>
public sealed record TokenFilterRequest
{
    /// <summary>Gets or sets requested 1-indexed page index.</summary>
    public int PageIndex { get; init; } = 1;

    /// <summary>Gets or sets requested page size.</summary>
    public int PageSize { get; init; } = 10;

    /// <summary>Gets or sets optional search term across token ID, reference ID, client ID, or user ID.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>Gets or sets user/subject filter.</summary>
    public string? UserId { get; init; }

    /// <summary>Gets or sets client ID filter.</summary>
    public string? ClientId { get; init; }

    private readonly DateTimeOffset? _createdFrom;
    private readonly DateTimeOffset? _createdTo;

    /// <summary>Gets or sets creation start date filter (inclusive) in UTC.</summary>
    public DateTimeOffset? CreatedFrom
    {
        get => _createdFrom;
        init => _createdFrom = value?.ToUniversalTime();
    }

    /// <summary>Gets or sets creation end date filter (inclusive) in UTC.</summary>
    public DateTimeOffset? CreatedTo
    {
        get => _createdTo;
        init => _createdTo = value?.ToUniversalTime();
    }

    /// <summary>Gets or sets token status filter (e.g. active, revoked).</summary>
    public string? Status { get; init; }

    /// <summary>Gets or sets token type filter (e.g. access_token, refresh_token).</summary>
    public string? TokenType { get; init; }
}
