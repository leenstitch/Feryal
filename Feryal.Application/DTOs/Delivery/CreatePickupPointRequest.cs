namespace Feryal.Application.DTOs.Delivery;

public sealed class CreatePickupPointRequest
{


    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public decimal PickupFee { get; set; }
}