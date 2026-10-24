namespace Feryal.Application.DTOs.Delivery;

public sealed class CreatePickupPointRequest
{
    // Gets or sets the name of the pickup point to be created.
    public string Name { get; set; } = string.Empty;

    // Gets or sets the address of the pickup point.
    public string Address { get; set; } = string.Empty;

    // Gets or sets the pickup fee charged for the pickup point.
    public decimal PickupFee { get; set; }
}