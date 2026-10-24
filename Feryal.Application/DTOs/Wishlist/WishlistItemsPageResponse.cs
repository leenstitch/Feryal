using Feryal.Application.DTOs.Wishlist;

namespace Feryal.Application.DTOs.Wishlist;

public sealed class WishlistItemsPageResponse
{
    public List<WishlistItemResponse> Items { get; set; } = new();

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}