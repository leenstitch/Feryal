namespace Feryal.Application.DTOs.Cart;

public sealed class CartPageResponse
{
    public List<CartItemResponse> Items { get; set; } = new();

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalQuantity { get; set; }
}