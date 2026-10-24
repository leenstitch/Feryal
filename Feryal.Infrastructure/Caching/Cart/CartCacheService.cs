using Feryal.Application.DTOs.Cart;
using Feryal.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Cart;

public sealed class CartCacheService : ICartCacheService
{
    private readonly HybridCache _hybridCache;
    private readonly ICartRepository _cartRepository;

    private const string CacheTagPrefix = "cart:user:";
    private const string CacheKeyPrefix = "cart:v1";

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    public CartCacheService(
        HybridCache hybridCache,
        ICartRepository cartRepository)
    {
        _hybridCache = hybridCache;
        _cartRepository = cartRepository;
    }

    private static string GetUserTag(int userId)
    {
        return $"{CacheTagPrefix}{userId}";
    }

    private static string GetItemsKey(
        int userId,
        int pageNumber,
        int pageSize)
    {
        return $"{CacheKeyPrefix}:user:{userId}:page:{pageNumber}:size:{pageSize}";
    }

    public async Task<CartPageResponse> GetItemsAsync(
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

        var cacheKey = GetItemsKey(
            userId,
            pageNumber,
            pageSize);

        var tags = new[]
        {
            GetUserTag(userId)
        };

        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var (items, totalCount, totalQuantity) =
                    await _cartRepository.GetPagedItemsAsync(
                        userId,
                        pageNumber,
                        pageSize,
                        token);

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

    public async Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        await _hybridCache.RemoveByTagAsync(
            GetUserTag(userId),
            cancellationToken);
    }
}