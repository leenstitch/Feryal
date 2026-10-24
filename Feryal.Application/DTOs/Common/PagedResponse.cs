
namespace Feryal.Application.DTOs.Common;

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalPages =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}