using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Cart;

public sealed class ChangeCartQuantityRequest
{
    // Gets or sets the quantity to increase or decrease for a cart item.
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}