using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Cart;

public sealed class ChangeCartQuantityRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}