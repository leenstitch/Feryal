using Feryal.Application.DTOs.Cart;
using Feryal.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Cart;

// Provides hybrid caching for cart items to reduce repeated database queries.
public sealed class CartCacheService : ICartCacheService
{
    // Handles cache storage and retrieval across local and distributed cache layers.
    private readonly HybridCache _hybridCache;

    // Retrieves paginated cart items from the data source when the cache misses.
    private readonly ICartRepository _cartRepository;

    // Prefix used to group cache entries by user for targeted invalidation.
    private const string CacheTagPrefix = "cart:user:";

    // Prefix used to version cart cache keys when their structure changes.
    private const string CacheKeyPrefix = "cart:v1";

    // Defines the expiration periods for distributed and local cache entries.
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    // Initializes the cache service with the hybrid cache and cart repository.
    public CartCacheService(
        HybridCache hybridCache,
        ICartRepository cartRepository)
    {
        _hybridCache = hybridCache;
        _cartRepository = cartRepository;
    }

    // Creates a cache tag shared by all cart entries belonging to a user.
    private static string GetUserTag(int userId)
    {
        return $"{CacheTagPrefix}{userId}";
    }

    // Builds a unique cache key for a user's cart page and pagination settings.
    private static string GetItemsKey(
        int userId,
        int pageNumber,
        int pageSize)
    {
        return $"{CacheKeyPrefix}:user:{userId}:page:{pageNumber}:size:{pageSize}";
    }

    // Retrieves a cached cart page or loads the data from the repository on a cache miss.
    public async Task<CartPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Ensure the user ID is valid.
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        // Ensure the requested page number is positive.
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        // Ensure the page size is positive.
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        // Generate the cache key for the requested cart page.
        var cacheKey = GetItemsKey(
            userId,
            pageNumber,
            pageSize);

        // Associate the entry with the user's tag for targeted invalidation.
        var tags = new[]
        {
            GetUserTag(userId)
        };

        // Return the cached response if available; otherwise, retrieve and cache it.
        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                // Fetch the requested cart page and its aggregate counts from the repository.
                var (items, totalCount, totalQuantity) =
                    await _cartRepository.GetPagedItemsAsync(
                        userId,
                        pageNumber,
                        pageSize,
                        token);

                // Construct the paginated response using the retrieved cart data.
                return new CartPageResponse
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalQuantity = totalQuantity
                };
            },
            CacheOptions,
            tags,
            cancellationToken);
    }

    // Removes all cached cart entries associated with the specified user.
    public async Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        // Ensure the user ID is valid before invalidating the cache.
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        // Invalidate entries tagged with this user's cart tag.
        await _hybridCache.RemoveByTagAsync(
            GetUserTag(userId),
            cancellationToken);
    }
}