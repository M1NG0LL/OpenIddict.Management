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

    private readonly string? _search;

    /// <summary>Gets or sets optional search term across token ID, reference ID, client ID, or user ID.</summary>
    public string? Search
    {
        get => _search ?? SearchTerm;
        init => _search = value;
    }

    /// <summary>Gets or sets optional search term across token ID, reference ID, client ID, or user ID (alias for <see cref="Search"/>).</summary>
    public string? SearchTerm
    {
        get => _search;
        init => _search = value;
    }

    /// <summary>Gets or sets user/subject filter.</summary>
    public string? UserId { get; init; }

    /// <summary>Gets or sets client ID filter.</summary>
    public string? ClientId { get; init; }

    /// <summary>Gets or sets authorization ID filter.</summary>
    public string? AuthorizationId { get; init; }

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

    /// <summary>Gets or sets optional sort field name (e.g. "CreationDate", "ExpirationDate", "Subject", "ClientId", "Type", "Status").</summary>
    public string? SortBy { get; init; }

    /// <summary>Gets or sets a value indicating whether to sort descending. Defaults to true for token timelines.</summary>
    public bool SortDescending { get; init; } = true;

    /// <summary>
    /// Returns a copy of the request configured with the specified sort parameters.
    /// </summary>
    public TokenFilterRequest WithSort(string sortBy, bool sortDescending = false) =>
        this with { SortBy = sortBy, SortDescending = sortDescending };
}
