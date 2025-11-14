namespace PRFactory.Domain.DTOs;

/// <summary>
/// Parameters for paginating and filtering query results.
/// </summary>
public class PaginationParams
{
    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the search query for filtering results.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the property name to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to sort in descending order.
    /// </summary>
    public bool Descending { get; set; } = true;
}
