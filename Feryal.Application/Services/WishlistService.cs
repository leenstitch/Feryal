using Feryal.Application.DTOs.Wishlist;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using WishlistEntity = Feryal.Domain.Entities.Wishlist;

namespace Feryal.Application.Wishlist.Services;

// Handles wishlist operations, including retrieving, adding, and removing items.
public sealed class WishlistService : IWishlistService
{
    // Provides database operations for wishlists and their items.
    private readonly IWishlistRepository _wishlistRepository;

    // Retrieves cached wishlist data and invalidates it when changes occur.
    private readonly IWishlistCacheService _wishlistCacheService;

    // Coordinates saving changes to the database.
    private readonly IUnitOfWork _unitOfWork;

    // Initializes the service with its required dependencies.
    public WishlistService(
        IWishlistRepository wishlistRepository,
        IWishlistCacheService wishlistCacheService,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _wishlistCacheService = wishlistCacheService;
        _unitOfWork = unitOfWork;
    }

    // Retrieves a paginated list of wishlist items for a user.
    public async Task<WishlistItemsPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Ensure the user ID is valid before retrieving wishlist data.
        ValidateUserId(userId);

        // Validate the requested page number.
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber));
        }

        // Validate the requested page size.
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize));
        }

        // Retrieve wishlist items through the cache service.
        return await _wishlistCacheService.GetItemsAsync(
            userId,
            pageNumber,
            pageSize,
            cancellationToken);
    }

    // Adds a product variant to the user's wishlist.
    public async Task AddItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);

        // Ensure the product variant ID is valid.
        if (productVariantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productVariantId));
        }

        // Retrieve the variant and its product details for availability checks.
        var variant =
            await _wishlistRepository.GetVariantForWishlistAsync(
                productVariantId,
                cancellationToken);

        // Only active products with active, configured variants can be added.
        if (variant is null ||
            variant.Product is null ||
            !variant.Product.IsActive ||
            !variant.IsActive ||
            !variant.IsConfigured)
        {
            throw new KeyNotFoundException(
                "The product variant does not exist or is unavailable.");
        }

        // Retrieve the user's wishlist for updating.
        var wishlist =
            await _wishlistRepository.GetByUserIdForUpdateAsync(
                userId,
                cancellationToken);

        // Create a wishlist if the user does not already have one.
        if (wishlist is null)
        {
            wishlist = new WishlistEntity(userId);

            await _wishlistRepository.AddWishlistAsync(
                wishlist,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        // Check whether this variant is already in the wishlist.
        var alreadyExists =
            await _wishlistRepository.ContainsVariantAsync(
                wishlist.Id,
                productVariantId,
                cancellationToken);

        // Avoid adding the same variant more than once.
        if (alreadyExists)
        {
            return;
        }

        // Create a wishlist item linked to the selected variant.
        var item = new WishlistItem(
            wishlist.Id,
            productVariantId);

        wishlist.AddItem(item);

        // Persist the new wishlist item.
        await _wishlistRepository.AddItemAsync(
            item,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        // Remove cached wishlist data so subsequent requests retrieve updated items.
        await _wishlistCacheService.InvalidateUserAsync(
            userId,
            cancellationToken);
    }

    // Removes a product variant from the user's wishlist.
    public async Task RemoveItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);

        // Ensure the product variant ID is valid.
        if (productVariantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productVariantId));
        }

        // Retrieve the user's wishlist for updating.
        var wishlist =
            await _wishlistRepository.GetByUserIdForUpdateAsync(
                userId,
                cancellationToken);

        // Stop if the user does not have a wishlist.
        if (wishlist is null)
        {
            throw new KeyNotFoundException(
                "Wishlist was not found.");
        }

        // Find the requested variant in the wishlist items.
        var item = wishlist.Items.FirstOrDefault(
            x => x.ProductVariantId == productVariantId);

        // Stop if the requested variant is not in the wishlist.
        if (item is null)
        {
            throw new KeyNotFoundException(
                "The product variant was not found in the wishlist.");
        }

        // Remove the item from the wishlist entity.
        wishlist.RemoveItem(item);

        // Delete the wishlist item from the repository.
        await _wishlistRepository.RemoveItemAsync(
            item,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        // Invalidate cached wishlist data after the removal.
        await _wishlistCacheService.InvalidateUserAsync(
            userId,
            cancellationToken);
    }

    // Validates that the user ID is a positive integer.
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