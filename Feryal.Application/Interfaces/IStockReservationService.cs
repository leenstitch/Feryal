namespace Feryal.Application.Interfaces;

public interface IStockReservationService
{
    Task<bool> TryReserveAsync(
        int variantId,
        int quantity,
        CancellationToken cancellationToken);
}