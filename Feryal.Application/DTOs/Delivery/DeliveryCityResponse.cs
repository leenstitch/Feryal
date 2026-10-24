namespace Feryal.Application.DTOs.Delivery;

public sealed class DeliveryCityResponse
{
    // Gets or sets the unique identifier of the delivery city.
    public int Id { get; set; }

    // Gets or sets the name of the delivery city.
    public string Name { get; set; } = string.Empty;

    // Gets or sets the delivery fee for the city.
    public decimal DeliveryFee { get; set; }

    // Gets or sets a value indicating whether the delivery city is active.
    public bool IsActive { get; set; }

    // Gets or sets the date and time when the delivery city was created.
    public DateTimeOffset CreatedAt { get; set; }

    // Gets or sets the date and time when the delivery city was last updated.
    public DateTimeOffset? UpdatedAt { get; set; }
}