using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Cart;

public sealed class AddCartItemRequest
{
    // Gets or sets the ID of the product variant to add to the cart.
    [Range(1, int.MaxValue)]
    public int ProductVariantId { get; set; }

    // Gets or sets the quantity of the product variant to add to the cart.
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}