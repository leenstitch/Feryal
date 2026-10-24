using Feryal.Application.DTOs.Cart;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _context;

    public CartRepository(ApplicationDbContext context)
    {
        _context = context;
    }

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

    public async Task<(
        List<CartItemResponse> Items,
        int TotalCount,
        int TotalQuantity)> GetPagedItemsAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        var query = _context.Set<CartItem>()
            .AsNoTracking()
            .Where(item => item.Cart.UserId == userId);

        var totalCount = await query.CountAsync(
            cancellationToken);

        var totalQuantity = await query
            .SumAsync(
                item => (int?)item.Quantity,
                cancellationToken) ?? 0;

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

        var items = cartItems.Select(item =>
        {
            var variant = item.ProductVariant;
            var product = variant.Product;
            var finalPrice = variant.FinalPrice;

            return new CartItemResponse
            {
                Id = item.Id,

                ProductId = variant.ProductId,
                ProductName = product.Name,

                PrimaryImageUrl = product.Images
                    .Where(image => image.IsPrimary)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),

                ProductVariantId = variant.Id,
                SKU = variant.SKU,
                Description = variant.Description,

                ColorId = variant.ColorId,
                ColorName = variant.Color?.Name,
                ColorHexCode = variant.Color?.HexCode,

                SizeId = variant.SizeId,
                SizeName = variant.Size?.Name,

                ScentId = variant.ScentId,
                ScentName = variant.Scent?.Name,

                Price = variant.Price,
                DiscountPercentage = variant.DiscountPercentage,
                FinalPrice = finalPrice,

                Quantity = item.Quantity,
                StockQuantity = variant.StockQuantity,

                LineTotal = finalPrice.HasValue
                    ? finalPrice.Value * item.Quantity
                    : null,

                IsProductActive = product.IsActive,
                IsVariantActive = variant.IsActive,
                IsConfigured = variant.IsConfigured,
                IsInStock = variant.IsInStock
            };
        }).ToList();

        return (items, totalCount, totalQuantity);
    }

    public async Task AddCartAsync(
        Cart cart,
        CancellationToken cancellationToken)
    {
        await _context.Set<Cart>().AddAsync(
            cart,
            cancellationToken);
    }

    public async Task AddItemAsync(
        CartItem item,
        CancellationToken cancellationToken)
    {
        await _context.Set<CartItem>().AddAsync(
            item,
            cancellationToken);
    }

    public Task RemoveItemAsync(
        CartItem item,
        CancellationToken cancellationToken)
    {
        _context.Set<CartItem>().Remove(item);

        return Task.CompletedTask;
    }
}