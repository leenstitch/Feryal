using Feryal.Application.Interfaces;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Inventory;

// Handles stock reservations by atomically reducing the available quantity of a product variant.
public sealed class StockReservationService : IStockReservationService
{
    private readonly ApplicationDbContext _dbContext;

    // Inject the database context to access product variants.
    public StockReservationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Attempts to reserve the requested quantity of an eligible product variant.
    public async Task<bool> TryReserveAsync(
        int variantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        // Ensure the variant ID is valid.
        if (variantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(variantId),
                "Variant ID must be greater than zero.");
        }

        // Ensure the reservation quantity is positive.
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        // Atomically decrease stock only when the variant meets all reservation conditions.
        // ExecuteUpdateAsync performs the update directly in the database without loading the entity.
        var affectedRows = await _dbContext.ProductVariants
            .Where(variant =>
                variant.Id == variantId &&
                variant.IsActive &&
                variant.IsConfigured &&
                variant.Price.HasValue &&
                variant.StockQuantity.HasValue &&
                variant.StockQuantity.Value >= quantity &&
                variant.Product.IsActive)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    variant => variant.StockQuantity,
                    variant => variant.StockQuantity!.Value - quantity),
                cancellationToken);

        // Return true only when exactly one variant was updated successfully.
        // Return false if the variant does not exist or fails any reservation condition.
        return affectedRows == 1;
    }
}