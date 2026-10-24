using Feryal.Application.DTOs.Wishlist;
using Feryal.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Wishlist;

public sealed class WishlistCacheService : IWishlistCacheService
{
    private readonly HybridCache _hybridCache;
    private readonly IWishlistRepository _wishlistRepository;

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    private const string AllWishlistsTag = "wishlists:all";

    public WishlistCacheService(
        HybridCache hybridCache,
        IWishlistRepository wishlistRepository)
    {
        _hybridCache = hybridCache;
        _wishlistRepository = wishlistRepository;
    }

    private static string GetUserTag(int userId)
    {
        return $"wishlist:user:{userId}";
    }

    private static string GetItemsKey(
        int userId,
        int pageNumber,
        int pageSize)
    {
        return $"wishlist:v2:user:{userId}:items:page:{pageNumber}:size:{pageSize}";
    }

    public async Task<WishlistItemsPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "User ID must be greater than zero.");
        }

        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "Page number must be greater than zero.");
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be greater than zero.");
        }

        var cacheKey = GetItemsKey(userId, pageNumber, pageSize);

        var tags = new[]
        {
            AllWishlistsTag,
            GetUserTag(userId)
        };

        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var (items, totalCount) =
                    await _wishlistRepository.GetPagedVisibleItemsAsync(
                        userId,
                        pageNumber,
                        pageSize,
                        token);

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

    public async Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "User ID must be greater than zero.");
        }

        await _hybridCache.RemoveByTagAsync(
            GetUserTag(userId),
            cancellationToken);
    }

    public async Task InvalidateAllAsync(
        CancellationToken cancellationToken)
    {
        await _hybridCache.RemoveByTagAsync(
            AllWishlistsTag,
            cancellationToken);
    }
}