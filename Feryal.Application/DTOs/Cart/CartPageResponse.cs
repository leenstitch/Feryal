namespace Feryal.Application.DTOs.Cart;

public sealed class CartPageResponse
{
    // Gets or sets the list of cart items on the current page.
    public List<CartItemResponse> Items { get; set; } = new();

    // Gets or sets the current page number.
    public int PageNumber { get; set; }

    // Gets or sets the maximum number of items displayed per page.
    public int PageSize { get; set; }

    // Gets or sets the total number of cart items across all pages.
    public int TotalCount { get; set; }

    // Gets or sets the total quantity of products in the cart.
    public int TotalQuantity { get; set; }
}