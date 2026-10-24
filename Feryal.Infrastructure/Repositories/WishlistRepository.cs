using Feryal.Application.DTOs.Wishlist;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public sealed class WishlistRepository : IWishlistRepository
{
    private readonly ApplicationDbContext _context;

    public WishlistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Wishlist?> GetByUserIdForUpdateAsync(
       int userId,
       CancellationToken cancellationToken)
    {
        return _context.Set<Wishlist>()
            .Include(w => w.Items
                .Where(i => i.ProductVariant.IsConfigured))
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .FirstOrDefaultAsync(
                w => w.UserId == userId,
                cancellationToken);
    }

    public Task<ProductVariant?> GetVariantForWishlistAsync(
       int productVariantId,
       CancellationToken cancellationToken)
    {
        return _context.Set<ProductVariant>()
            .AsNoTracking()
            .Include(v => v.Product)
                .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            .FirstOrDefaultAsync(
                v => v.Id == productVariantId &&
                     v.IsConfigured,
                cancellationToken);
    }

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

    public async Task<(List<WishlistItemResponse> Items, int TotalCount)>
        GetPagedVisibleItemsAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        var query = _context.Set<WishlistItem>()
            .AsNoTracking()
            .Where(item =>
                item.Wishlist.UserId == userId &&
                item.ProductVariant.Product.IsActive &&
                item.ProductVariant.IsActive &&
                item.ProductVariant.IsConfigured);

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderByDescending(item => item.AddedAt)
            .ThenByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new WishlistItemResponse
            {
                Id = item.Id,

                ProductId = item.ProductVariant.ProductId,

                ProductName = item.ProductVariant.Product.Name,

                PrimaryImageUrl = item.ProductVariant.Product.Images
                    .Where(image => image.IsPrimary)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),

                ProductVariantId = item.ProductVariantId,

                SKU = item.ProductVariant.SKU,

                Description = item.ProductVariant.Description,

                ColorId = item.ProductVariant.ColorId,

                ColorName = item.ProductVariant.Color != null
                    ? item.ProductVariant.Color.Name
                    : null,

                ColorHexCode = item.ProductVariant.Color != null
                    ? item.ProductVariant.Color.HexCode
                    : null,

                SizeId = item.ProductVariant.SizeId,

                SizeName = item.ProductVariant.Size != null
                    ? item.ProductVariant.Size.Name
                    : null,

                ScentId = item.ProductVariant.ScentId,

                ScentName = item.ProductVariant.Scent != null
                    ? item.ProductVariant.Scent.Name
                    : null,

                Price = item.ProductVariant.Price,

                DiscountPercentage =
                    item.ProductVariant.DiscountPercentage,

                FinalPrice = item.ProductVariant.Price == null
                    ? null
                    : item.ProductVariant.Price.Value *
                      (100m - item.ProductVariant.DiscountPercentage) /
                      100m,

                IsVariantActive = item.ProductVariant.IsActive,
                StockQuantity = item.ProductVariant.StockQuantity,
                IsInStock =
                    item.ProductVariant.StockQuantity.HasValue &&
                    item.ProductVariant.StockQuantity.Value > 0,

                AddedAt = item.AddedAt
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddWishlistAsync(
        Wishlist wishlist,
        CancellationToken cancellationToken)
    {
        await _context.Set<Wishlist>().AddAsync(
            wishlist,
            cancellationToken);
    }

    public async Task AddItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken)
    {
        await _context.Set<WishlistItem>().AddAsync(
            item,
            cancellationToken);
    }

    public Task RemoveItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken)
    {
        _context.Set<WishlistItem>().Remove(item);

        return Task.CompletedTask;
    }
}