using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Cart;

public sealed class AddCartItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductVariantId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}