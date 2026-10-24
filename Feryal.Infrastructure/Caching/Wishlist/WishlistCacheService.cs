using Feryal.Application.DTOs.Wishlist;
using Feryal.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Wishlist;

// Provides hybrid caching for wishlist items and supports targeted cache invalidation.
public sealed class WishlistCacheService : IWishlistCacheService
{
    // Handles cache storage, retrieval, and invalidation.
    private readonly HybridCache _hybridCache;

    // Retrieves wishlist items from the repository when the cache misses.
    private readonly IWishlistRepository _wishlistRepository;

    // Defines the expiration periods for distributed and local cache entries.
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    // Tag used to group all wishlist cache entries for global invalidation.
    private const string AllWishlistsTag = "wishlists:all";

    // Initializes the service with the hybrid cache and wishlist repository.
    public WishlistCacheService(
        HybridCache hybridCache,
        IWishlistRepository wishlistRepository)
    {
        _hybridCache = hybridCache;
        _wishlistRepository = wishlistRepository;
    }

    // Generates a cache tag associated with a specific user's wishlist.
    private static string GetUserTag(int userId)
    {
        return $"wishlist:user:{userId}";
    }

    // Builds a unique cache key for a user's wishlist page.
    private static string GetItemsKey(
        int userId,
        int pageNumber,
        int pageSize)
    {
        return $"wishlist:v2:user:{userId}:items:page:{pageNumber}:size:{pageSize}";
    }

    // Retrieves a paginated wishlist response from the cache or repository.
    public async Task<WishlistItemsPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Ensure the user ID is valid.
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "User ID must be greater than zero.");
        }

        // Ensure the requested page number is positive.
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "Page number must be greater than zero.");
        }

        // Ensure the page size is positive.
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be greater than zero.");
        }

        // Generate the cache key using the user ID and pagination settings.
        var cacheKey = GetItemsKey(userId, pageNumber, pageSize);

        // Associate the entry with global and user-specific invalidation tags.
        var tags = new[]
        {
            AllWishlistsTag,
            GetUserTag(userId)
        };

        // Return the cached response or retrieve and cache the wishlist items.
        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                // Retrieve the visible wishlist items and their total count.
                var (items, totalCount) =
                    await _wishlistRepository.GetPagedVisibleItemsAsync(
                        userId,
                        pageNumber,
                        pageSize,
                        token);

                // Build the paginated response using the retrieved items and metadata.
                return new WishlistItemsPageResponse
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            },
            CacheOptions,
            tags,
            cancellationToken);
    }

    // Invalidates all cached wishlist entries associated with the specified user.
    public async Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        // Ensure the user ID is valid before invalidating the cache.
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "User ID must be greater than zero.");
        }

        // Remove cache entries associated with this user's wishlist tag.
        await _hybridCache.RemoveByTagAsync(
            GetUserTag(userId),
            cancellationToken);
    }

    // Invalidates all cached wishlist entries across users.
    public async Task InvalidateAllAsync(
        CancellationToken cancellationToken)
    {
        // Remove cache entries associated with the global wishlist tag.
        await _hybridCache.RemoveByTagAsync(
            AllWishlistsTag,
            cancellationToken);
    }
}