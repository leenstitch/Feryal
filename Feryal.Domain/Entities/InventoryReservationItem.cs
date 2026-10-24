namespace Feryal.Domain.Entities;

public class InventoryReservationItem
{
    private InventoryReservationItem()
    {
    }

    public InventoryReservationItem(
        int reservationId,
        int productVariantId,
        int quantity)
    {
        if (reservationId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reservationId));
        }

        if (productVariantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productVariantId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ReservationId = reservationId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
    }

    public int Id { get; private set; }

    public int ReservationId { get; private set; }

    public int ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    public InventoryReservation Reservation { get; private set; } = null!;

    public ProductVariant ProductVariant { get; private set; } = null!;
}