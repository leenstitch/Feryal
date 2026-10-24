namespace Feryal.Application.DTOs.Delivery;

public sealed class PickupPointResponse
{
    public int Id { get; set; }

   

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public decimal PickupFee { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}