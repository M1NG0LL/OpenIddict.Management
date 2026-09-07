namespace OpenIddict.Management.Dto;

/// <summary>
/// Represents pagination and filtering request parameters.
/// </summary>
public sealed record PagedRequest
{
    private readonly int _pageIndex = 1;
    private readonly int _pageSize = 20;

    /// <summary>
    /// Gets the 1-indexed page number. Defaults to 1.
    /// </summary>
    public int PageIndex
    {
        get => _pageIndex;
        init => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Gets the number of items per page. Defaults to 20, max 100.
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
    /// Gets an optional search term to filter results.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Gets an optional field name to sort results by.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets a value indicating whether sorting should be in descending order.
    /// </summary>
    public bool SortDescending { get; init; }

    /// <summary>
    /// Creates a copy of the <see cref="PagedRequest"/> with a strongly-typed enum sort field.
    /// </summary>
    /// <typeparam name="TSortEnum">The sort field enum type.</typeparam>
    /// <param name="sortEnum">The enum sort value.</param>
    /// <param name="sortDescending">Whether to sort in descending order.</param>
    /// <returns>A new <see cref="PagedRequest"/> instance.</returns>
    public PagedRequest WithSort<TSortEnum>(TSortEnum sortEnum, bool sortDescending = false)
        where TSortEnum : struct, Enum
    {
        return this with
        {
            SortBy = sortEnum.ToString(),
            SortDescending = sortDescending
        };
    }
}
