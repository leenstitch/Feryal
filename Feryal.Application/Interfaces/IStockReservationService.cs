namespace Feryal.Application.Interfaces;

// Defines the contract for reserving stock for a product variant.
public interface IStockReservationService
{
    // Attempts to reserve the specified quantity of a product variant's stock.
    Task<bool> TryReserveAsync(
        int variantId,
        int quantity,
        CancellationToken cancellationToken);
}