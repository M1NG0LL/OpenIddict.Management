namespace OpenIddict.Management.Dto;

/// <summary>
/// Criteria for filtering, searching, and paginating audit trail entries.
/// </summary>
public sealed record AuditFilterRequest
{
    private readonly int _pageIndex = 1;
    private readonly int _pageSize = 20;

    /// <summary>
    /// Gets or sets the 1-indexed page number. Defaults to 1.
    /// </summary>
    public int PageIndex
    {
        get => _pageIndex;
        init => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Gets or sets the number of items per page. Defaults to 20, max 100.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => 1,
            > 100 => 100,
            _ => value
        };
    }

    /// <summary>
    /// Gets or sets an optional search term across category, action, entity name, entity ID, actor, or details.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Gets or sets optional category filter (e.g., "Application", "Scope", "Token", "Authorization").
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets or sets optional action filter (e.g., "Created", "Updated", "Deleted", "Revoked").
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Gets or sets optional entity identifier filter.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Gets or sets optional actor filter.
    /// </summary>
    public string? Actor { get; init; }

    /// <summary>
    /// Gets or sets optional success filter.
    /// </summary>
    public bool? Success { get; init; }

    /// <summary>
    /// Gets or sets optional starting timestamp (inclusive) in UTC.
    /// </summary>
    public DateTimeOffset? FromDate { get; init; }

    /// <summary>
    /// Gets or sets optional ending timestamp (inclusive) in UTC.
    /// </summary>
    public DateTimeOffset? ToDate { get; init; }

    /// <summary>
    /// Gets or sets the field to sort by. Defaults to "Timestamp".
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether sorting is in descending order. Defaults to true.
    /// </summary>
    public bool SortDescending { get; init; } = true;
}
