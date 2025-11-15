namespace PRFactory.Domain.DTOs;

/// <summary>
/// Represents a paginated result set with metadata about the pagination.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets or sets the items for the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates a new empty paged result.
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Creates a new paged result with the specified values.
    /// </summary>
    public PagedResult(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        Page = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
