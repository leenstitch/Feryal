
namespace Feryal.Domain.Entities;

public class PickupPoint
{
    private PickupPoint()
    {
    }

    public PickupPoint(string name, string address, decimal pickupFee)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pickup point name is required.", nameof(name));

        if (name.Trim().Length > 150)
            throw new ArgumentException("Pickup point name cannot exceed 150 characters.", nameof(name));

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Pickup point address is required.", nameof(address));

        if (address.Trim().Length > 500)
            throw new ArgumentException("Pickup point address cannot exceed 500 characters.", nameof(address));

        if (pickupFee < 0)
            throw new ArgumentOutOfRangeException(nameof(pickupFee), "Pickup fee cannot be negative.");

        Name = name.Trim();
        Address = address.Trim();
        PickupFee = pickupFee;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Address { get; private set; } = null!;

    public decimal PickupFee { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(string name, string address, decimal pickupFee)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pickup point name is required.", nameof(name));

        if (name.Trim().Length > 150)
            throw new ArgumentException("Pickup point name cannot exceed 150 characters.", nameof(name));

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Pickup point address is required.", nameof(address));

        if (address.Trim().Length > 500)
            throw new ArgumentException("Pickup point address cannot exceed 500 characters.", nameof(address));

        if (pickupFee < 0)
            throw new ArgumentOutOfRangeException(nameof(pickupFee), "Pickup fee cannot be negative.");

        Name = name.Trim();
        Address = address.Trim();
        PickupFee = pickupFee;
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
