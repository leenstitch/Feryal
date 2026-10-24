namespace Feryal.Application.DTOs.Delivery;

public sealed class DeliveryCityResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal DeliveryFee { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}