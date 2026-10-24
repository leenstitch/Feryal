namespace Feryal.Application.DTOs.Delivery;

public sealed class CreateDeliveryCityRequest
{
    public string Name { get; set; } = string.Empty;

    public decimal DeliveryFee { get; set; }
}