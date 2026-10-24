using Feryal.Domain.Enums;

namespace Feryal.Domain.Entities;

public class InventoryReservation
{
    private readonly List<InventoryReservationItem> _items = new();

    private InventoryReservation()
    {
    }

    public InventoryReservation(
        int userId,
        DateTimeOffset expiresAt,
        string reservationToken,
        string idempotencyKey)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException(
                "Expiration must be in the future.",
                nameof(expiresAt));
        }

        if (string.IsNullOrWhiteSpace(reservationToken))
        {
            throw new ArgumentException(
                "Reservation token is required.",
                nameof(reservationToken));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));
        }

        UserId = userId;
        ExpiresAt = expiresAt;
        ReservationToken = reservationToken;
        IdempotencyKey = idempotencyKey;
        Status = InventoryReservationStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int UserId { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public InventoryReservationStatus Status { get; private set; }

    public string ReservationToken { get; private set; } = null!;

    public string IdempotencyKey { get; private set; } = null!;

    public int? OrderId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<InventoryReservationItem> Items =>
        _items.AsReadOnly();

    public void AddItem(InventoryReservationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (Status != InventoryReservationStatus.Active)
        {
            throw new InvalidOperationException(
                "Items can only be added to an active reservation.");
        }

        if (_items.Any(x => x.ProductVariantId == item.ProductVariantId))
        {
            throw new InvalidOperationException(
                "This variant already exists in the reservation.");
        }

        _items.Add(item);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}