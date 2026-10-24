using Feryal.Application.DTOs.Wishlist;

namespace Feryal.Application.DTOs.Wishlist;

// Represents a paginated response containing wishlist items and pagination details.
public sealed class WishlistItemsPageResponse
{
    // Gets or sets the list of wishlist items on the current page.
    public List<WishlistItemResponse> Items { get; set; } = new();

    // Gets or sets the current page number.
    public int PageNumber { get; set; }

    // Gets or sets the number of wishlist items per page.
    public int PageSize { get; set; }

    // Gets or sets the total number of wishlist items across all pages.
    public int TotalCount { get; set; }
}