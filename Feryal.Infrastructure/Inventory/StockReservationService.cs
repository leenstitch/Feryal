using Feryal.Application.Interfaces;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Inventory;

public sealed class StockReservationService : IStockReservationService
{
    private readonly ApplicationDbContext _dbContext;

    public StockReservationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryReserveAsync(
        int variantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(variantId),
                "Variant ID must be greater than zero.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

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

        return affectedRows == 1;
    }
}