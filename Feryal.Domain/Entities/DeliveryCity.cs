namespace Feryal.Domain.Entities;

public class DeliveryCity
{
    private readonly List<PickupPoint> _pickupPoints = new();

    private DeliveryCity()
    {
    }

    public DeliveryCity(
        string name,
        decimal deliveryFee)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "City name is required.",
                nameof(name));

        if (deliveryFee < 0)
            throw new ArgumentException(
                "Delivery fee cannot be negative.",
                nameof(deliveryFee));

        Name = name.Trim();
        DeliveryFee = deliveryFee;

        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public decimal DeliveryFee { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    

    public void Update(string name, decimal deliveryFee)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "City name is required.",
                nameof(name));

        if (deliveryFee < 0)
            throw new ArgumentException(
                "Delivery fee cannot be negative.",
                nameof(deliveryFee));

        Name = name.Trim();
        DeliveryFee = deliveryFee;

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}