using Feryal.Application.DTOs.Cart;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Handles database operations related to shopping carts and their items.
public sealed class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _context;

    // Inject the database context to access cart and product data.
    public CartRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Retrieves a user's cart and its items for update operations.
    public Task<Cart?> GetByUserIdForUpdateAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return _context.Set<Cart>()
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(
                cart => cart.UserId == userId,
                cancellationToken);
    }

    // Retrieves a product variant with its associated product for cart validation.
    // AsNoTracking avoids tracking the entity because this query is read-only.
    public Task<ProductVariant?> GetVariantForCartAsync(
        int productVariantId,
        CancellationToken cancellationToken)
    {
        return _context.Set<ProductVariant>()
            .AsNoTracking()
            .Include(variant => variant.Product)
            .FirstOrDefaultAsync(
                variant => variant.Id == productVariantId,
                cancellationToken);
    }

    // Retrieves a paginated list of cart items along with the total item count and quantity.
    public async Task<(
        List<CartItemResponse> Items,
        int TotalCount,
        int TotalQuantity)> GetPagedItemsAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        // Build a read-only query for the specified user's cart items.
        var query = _context.Set<CartItem>()
            .AsNoTracking()
            .Where(item => item.Cart.UserId == userId);

        // Count all cart item records before pagination.
        var totalCount = await query.CountAsync(
            cancellationToken);

        // Calculate the total quantity across all cart items.
        var totalQuantity = await query
            .SumAsync(
                item => (int?)item.Quantity,
                cancellationToken) ?? 0;

        // Retrieve the requested page, including the related product and variant details.
        var cartItems = await query
            .OrderByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(item => item.ProductVariant)
                .ThenInclude(variant => variant.Product)
                    .ThenInclude(product => product.Images)
            .Include(item => item.ProductVariant)
                .ThenInclude(variant => variant.Color)
            .Include(item => item.ProductVariant)
                .ThenInclude(variant => variant.Size)
            .Include(item => item.ProductVariant)
                .ThenInclude(variant => variant.Scent)
            .ToListAsync(cancellationToken);

        // Map the retrieved entities to response DTOs.
        var items = cartItems.Select(item =>
        {
            var variant = item.ProductVariant;
            var product = variant.Product;
            var finalPrice = variant.FinalPrice;

            return new CartItemResponse
            {
                // Cart item and product identification.
                Id = item.Id,
                ProductId = variant.ProductId,
                ProductName = product.Name,

                // Select the primary image URL, if one exists.
                PrimaryImageUrl = product.Images
                    .Where(image => image.IsPrimary)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),

                // Product variant details.
                ProductVariantId = variant.Id,
                SKU = variant.SKU,
                Description = variant.Description,

                // Selected color details.
                ColorId = variant.ColorId,
                ColorName = variant.Color?.Name,
                ColorHexCode = variant.Color?.HexCode,

                // Selected size details.
                SizeId = variant.SizeId,
                SizeName = variant.Size?.Name,

                // Selected scent details.
                ScentId = variant.ScentId,
                ScentName = variant.Scent?.Name,

                // Pricing information, including any applicable discount.
                Price = variant.Price,
                DiscountPercentage = variant.DiscountPercentage,
                FinalPrice = finalPrice,

                // Requested quantity and current available stock.
                Quantity = item.Quantity,
                StockQuantity = variant.StockQuantity,

                // Calculate the line total when a final price is available.
                LineTotal = finalPrice.HasValue
                    ? finalPrice.Value * item.Quantity
                    : null,

                // Availability and configuration status.
                IsProductActive = product.IsActive,
                IsVariantActive = variant.IsActive,
                IsConfigured = variant.IsConfigured,
                IsInStock = variant.IsInStock
            };
        }).ToList();

        // Return the current page and pagination summary information.
        return (items, totalCount, totalQuantity);
    }

    // Adds a new cart to the database context.
    public async Task AddCartAsync(
        Cart cart,
        CancellationToken cancellationToken)
    {
        await _context.Set<Cart>().AddAsync(
            cart,
            cancellationToken);
    }

    // Adds a new item to the database context.
    public async Task AddItemAsync(
        CartItem item,
        CancellationToken cancellationToken)
    {
        await _context.Set<CartItem>().AddAsync(
            item,
            cancellationToken);
    }

    // Marks a cart item for deletion from the database.
    public Task RemoveItemAsync(
        CartItem item,
        CancellationToken cancellationToken)
    {
        _context.Set<CartItem>().Remove(item);

        // SaveChangesAsync is handled separately by the Unit of Work.
        return Task.CompletedTask;
    }
}