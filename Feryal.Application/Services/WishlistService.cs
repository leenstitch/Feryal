using Feryal.Application.DTOs.Wishlist;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using WishlistEntity = Feryal.Domain.Entities.Wishlist;

namespace Feryal.Application.Wishlist.Services;

public sealed class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IWishlistCacheService _wishlistCacheService;
    private readonly IUnitOfWork _unitOfWork;

    public WishlistService(
        IWishlistRepository wishlistRepository,
        IWishlistCacheService wishlistCacheService,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _wishlistCacheService = wishlistCacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<WishlistItemsPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);

        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize));
        }

        return await _wishlistCacheService.GetItemsAsync(
            userId,
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public async Task AddItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);

        if (productVariantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productVariantId));
        }

        var variant =
            await _wishlistRepository.GetVariantForWishlistAsync(
                productVariantId,
                cancellationToken);

        if (variant is null ||
            variant.Product is null ||
            !variant.Product.IsActive ||
            !variant.IsActive ||
            !variant.IsConfigured)
        {
            throw new KeyNotFoundException(
                "The product variant does not exist or is unavailable.");
        }

        var wishlist =
            await _wishlistRepository.GetByUserIdForUpdateAsync(
                userId,
                cancellationToken);

        if (wishlist is null)
        {
            wishlist = new WishlistEntity(userId);

            await _wishlistRepository.AddWishlistAsync(
                wishlist,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        var alreadyExists =
            await _wishlistRepository.ContainsVariantAsync(
                wishlist.Id,
                productVariantId,
                cancellationToken);

        if (alreadyExists)
        {
            return;
        }

        var item = new WishlistItem(
            wishlist.Id,
            productVariantId);

        wishlist.AddItem(item);

        await _wishlistRepository.AddItemAsync(
            item,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        await _wishlistCacheService.InvalidateUserAsync(
            userId,
            cancellationToken);
    }

    public async Task RemoveItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);

        if (productVariantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productVariantId));
        }

        var wishlist =
            await _wishlistRepository.GetByUserIdForUpdateAsync(
                userId,
                cancellationToken);

        if (wishlist is null)
        {
            throw new KeyNotFoundException(
                "Wishlist was not found.");
        }

        var item = wishlist.Items.FirstOrDefault(
            x => x.ProductVariantId == productVariantId);

        if (item is null)
        {
            throw new KeyNotFoundException(
                "The product variant was not found in the wishlist.");
        }

        wishlist.RemoveItem(item);

        await _wishlistRepository.RemoveItemAsync(
            item,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        await _wishlistCacheService.InvalidateUserAsync(
            userId,
            cancellationToken);
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "User ID must be greater than zero.");
        }
    }
}