namespace OpenIddict.Management.Dto;

/// <summary>
/// Defines pagination metadata for paginated query results.
/// </summary>
public interface IPagedResult
{
    /// <summary>Gets the current 1-indexed page index.</summary>
    int PageIndex { get; }

    /// <summary>Gets the current 1-indexed page number (alias for <see cref="PageIndex"/>).</summary>
    int PageNumber { get; }

    /// <summary>Gets the page size.</summary>
    int PageSize { get; }

    /// <summary>Gets the total number of matching items across all pages.</summary>
    int TotalCount { get; }

    /// <summary>Gets the total number of pages.</summary>
    int TotalPages { get; }

    /// <summary>Gets a value indicating whether there is a previous page.</summary>
    bool HasPreviousPage { get; }

    /// <summary>Gets a value indicating whether there is a next page.</summary>
    bool HasNextPage { get; }
}

/// <summary>
/// Represents a paginated result set containing items of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public sealed record PagedResult<T> : IPagedResult
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Gets the current 1-indexed page index.
    /// </summary>
    public required int PageIndex { get; init; }

    /// <summary>
    /// Gets the current 1-indexed page number (alias for <see cref="PageIndex"/>).
    /// </summary>
    public int PageNumber => PageIndex;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of matching items across all pages.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;
}
