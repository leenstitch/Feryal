using Feryal.Application.DTOs.Wishlist;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Provides database operations for wishlists and their items.
public sealed class WishlistRepository : IWishlistRepository
{
    private readonly ApplicationDbContext _context;

    // Inject the database context to access wishlist data.
    public WishlistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Retrieves a user's wishlist for update operations, including configured variants and primary product images.
    public Task<Wishlist?> GetByUserIdForUpdateAsync(
       int userId,
       CancellationToken cancellationToken)
    {
        return _context.Set<Wishlist>()
            // Include only wishlist items whose variants are configured.
            .Include(w => w.Items
                .Where(i => i.ProductVariant.IsConfigured))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .FirstOrDefaultAsync(
                w => w.UserId == userId,
                cancellationToken);
    }

    // Retrieves a configured product variant and its product's primary images for wishlist operations.
    public Task<ProductVariant?> GetVariantForWishlistAsync(
       int productVariantId,
       CancellationToken cancellationToken)
    {
        return _context.Set<ProductVariant>()
            // Avoid tracking entities because this query is read-only.
            .AsNoTracking()
            .Include(v => v.Product)
                .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            .FirstOrDefaultAsync(
                v => v.Id == productVariantId &&
                     v.IsConfigured,
                cancellationToken);
    }

    // Checks whether a product variant already exists in the specified wishlist.
    public Task<bool> ContainsVariantAsync(
        int wishlistId,
        int productVariantId,
        CancellationToken cancellationToken)
    {
        return _context.Set<WishlistItem>()
            .AnyAsync(
                item =>
                    item.WishlistId == wishlistId &&
                    item.ProductVariantId == productVariantId,
                cancellationToken);
    }

    // Retrieves a paginated list of visible wishlist items for a user.
    public async Task<(List<WishlistItemResponse> Items, int TotalCount)>
        GetPagedVisibleItemsAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        // Validate the user ID.
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        // Validate the requested page number.
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        // Validate the requested page size.
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        // Build a read-only query for wishlist items whose products and variants are active and configured.
        var query = _context.Set<WishlistItem>()
            .AsNoTracking()
            .Where(item =>
                item.Wishlist.UserId == userId &&
                item.ProductVariant.Product.IsActive &&
                item.ProductVariant.IsActive &&
                item.ProductVariant.IsConfigured);

        // Count all matching items before applying pagination.
        var totalCount = await query.CountAsync(
            cancellationToken);

        // Retrieve the requested page, newest additions first, and project directly into response DTOs.
        var items = await query
            .OrderByDescending(item => item.AddedAt)
            .ThenByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new WishlistItemResponse
            {
                // Wishlist item identification.
                Id = item.Id,

                // Product information.
                ProductId = item.ProductVariant.ProductId,
                ProductName = item.ProductVariant.Product.Name,

                // Retrieve the primary product image URL, if available.
                PrimaryImageUrl = item.ProductVariant.Product.Images
                    .Where(image => image.IsPrimary)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),

                // Product variant information.
                ProductVariantId = item.ProductVariantId,
                SKU = item.ProductVariant.SKU,
                Description = item.ProductVariant.Description,

                // Selected color details.
                ColorId = item.ProductVariant.ColorId,

                ColorName = item.ProductVariant.Color != null
                    ? item.ProductVariant.Color.Name
                    : null,

                ColorHexCode = item.ProductVariant.Color != null
                    ? item.ProductVariant.Color.HexCode
                    : null,

                // Selected size details.
                SizeId = item.ProductVariant.SizeId,

                SizeName = item.ProductVariant.Size != null
                    ? item.ProductVariant.Size.Name
                    : null,

                // Selected scent details.
                ScentId = item.ProductVariant.ScentId,

                ScentName = item.ProductVariant.Scent != null
                    ? item.ProductVariant.Scent.Name
                    : null,

                // Pricing information.
                Price = item.ProductVariant.Price,
                DiscountPercentage =
                    item.ProductVariant.DiscountPercentage,

                // Calculate the discounted price when a base price exists.
                FinalPrice = item.ProductVariant.Price == null
                    ? null
                    : item.ProductVariant.Price.Value *
                      (100m - item.ProductVariant.DiscountPercentage) /
                      100m,

                // Variant availability and stock information.
                IsVariantActive = item.ProductVariant.IsActive,
                StockQuantity = item.ProductVariant.StockQuantity,

                IsInStock =
                    item.ProductVariant.StockQuantity.HasValue &&
                    item.ProductVariant.StockQuantity.Value > 0,

                // Record when the item was added to the wishlist.
                AddedAt = item.AddedAt
            })
            .ToListAsync(cancellationToken);

        // Return the current page and total number of visible wishlist items.
        return (items, totalCount);
    }

    // Adds a new wishlist to the database context.
    public async Task AddWishlistAsync(
        Wishlist wishlist,
        CancellationToken cancellationToken)
    {
        await _context.Set<Wishlist>().AddAsync(
            wishlist,
            cancellationToken);
    }

    // Adds a new item to the database context.
    public async Task AddItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken)
    {
        await _context.Set<WishlistItem>().AddAsync(
            item,
            cancellationToken);
    }

    // Marks a wishlist item for deletion from the database.
    public Task RemoveItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken)
    {
        _context.Set<WishlistItem>().Remove(item);

        // Saving changes is handled separately by the Unit of Work.
        return Task.CompletedTask;
    }
}