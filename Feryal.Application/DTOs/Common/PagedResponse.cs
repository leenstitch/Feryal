namespace Feryal.Application.DTOs.Common;

public class PagedResponse<T>
{
    // Gets or sets the list of items returned for the current page.
    public List<T> Items { get; set; } = new();

    // Gets or sets the total number of items across all pages.
    public int TotalCount { get; set; }

    // Gets or sets the current page number.
    public int PageNumber { get; set; } = 1;

    // Gets or sets the maximum number of items displayed per page.
    public int PageSize { get; set; } = 10;

    // Gets the total number of pages based on the total count and page size.
    public int TotalPages =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;

    // Gets a value indicating whether a previous page exists.
    public bool HasPreviousPage => PageNumber > 1;

    // Gets a value indicating whether a next page exists.
    public bool HasNextPage => PageNumber < TotalPages;
}