namespace Feryal.Application.DTOs.Delivery;

public sealed class PickupPointResponse
{
    // Gets or sets the unique identifier of the pickup point.
    public int Id { get; set; }



    // Gets or sets the name of the pickup point.
    public string Name { get; set; } = string.Empty;

    // Gets or sets the address of the pickup point.
    public string Address { get; set; } = string.Empty;

    // Gets or sets the pickup fee charged for the pickup point.
    public decimal PickupFee { get; set; }

    // Gets or sets a value indicating whether the pickup point is active.
    public bool IsActive { get; set; }

    // Gets or sets the date and time when the pickup point was created.
    public DateTimeOffset CreatedAt { get; set; }

    // Gets or sets the date and time when the pickup point was last updated.
    public DateTimeOffset? UpdatedAt { get; set; }
}