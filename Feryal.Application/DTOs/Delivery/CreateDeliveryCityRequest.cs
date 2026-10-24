namespace Feryal.Application.DTOs.Delivery;

public sealed class CreateDeliveryCityRequest
{
    // Gets or sets the name of the delivery city to be created.
    public string Name { get; set; } = string.Empty;

    // Gets or sets the delivery fee for the city.
    public decimal DeliveryFee { get; set; }
}