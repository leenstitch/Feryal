using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Delivery;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Caching.Delivery;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Delivery;

// Provides hybrid caching for delivery cities and pickup points.
public sealed class DeliverySettingsCacheService
    : IDeliverySettingsCacheService
{
    // Defines the expiration periods for distributed and local cache entries.
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    // Handles cache storage, retrieval, and invalidation.
    private readonly HybridCache _cache;

    // Retrieves paginated delivery city and pickup point data.
    private readonly IDeliverySettingsQueryRepository _queryRepository;

    // Retrieves individual delivery city entities.
    private readonly IRepository<DeliveryCity> _cityRepository;

    // Retrieves individual pickup point entities.
    private readonly IRepository<PickupPoint> _pickupPointRepository;

    // Initializes the service with the cache and required repositories.
    public DeliverySettingsCacheService(
        HybridCache cache,
        IDeliverySettingsQueryRepository queryRepository,
        IRepository<DeliveryCity> cityRepository,
        IRepository<PickupPoint> pickupPointRepository)
    {
        _cache = cache;
        _queryRepository = queryRepository;
        _cityRepository = cityRepository;
        _pickupPointRepository = pickupPointRepository;
    }

    // Retrieves a paginated list of delivery cities from the cache or repository.
    public async Task<PagedResponse<DeliveryCityResponse>> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        // Generate a cache key based on pagination and the active-only filter.
        string key = DeliveryCacheConstants.GetDeliveryCityListKey(
            pageNumber,
            pageSize,
            activeOnly);

        // Return the cached result or load and cache the requested city page.
        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                // Retrieve the requested page of delivery cities.
                var result = await _queryRepository.GetCitiesAsync(
                    pageNumber,
                    pageSize,
                    activeOnly,
                    token);

                // Convert delivery city entities into response DTOs.
                var items = result.Items
                    .Select(MapCity)
                    .ToList();

                // Construct the paginated response.
                return CreatePagedResponse(
                    items,
                    pageNumber,
                    pageSize,
                    result.TotalCount);
            },
            options: CacheOptions,
            // Tag the entry so all delivery city lists can be invalidated together.
            tags: new[]
            {
                DeliveryCacheConstants.DeliveryCityListsTag
            },
            cancellationToken: cancellationToken);
    }

    // Retrieves an individual delivery city from the cache or repository.
    public async Task<DeliveryCityResponse?> GetCityByIdAsync(
        int cityId,
        CancellationToken cancellationToken)
    {
        // Generate the cache key for the specified delivery city.
        string key =
            DeliveryCacheConstants.GetDeliveryCityDetailsKey(cityId);

        // Return the cached city or retrieve it from the repository on a cache miss.
        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                // Load the delivery city by its ID.
                DeliveryCity? city =
                    await _cityRepository.GetByIdAsync(cityId, token);

                // Return null if the city does not exist; otherwise, map it to a DTO.
                return city is null ? null : MapCity(city);
            },
            options: CacheOptions,
            // Associate the entry with a tag for city-specific invalidation.
            tags: new[]
            {
                DeliveryCacheConstants.GetDeliveryCityTag(cityId)
            },
            cancellationToken: cancellationToken);
    }

    // Retrieves a paginated list of pickup points from the cache or repository.
    public async Task<PagedResponse<PickupPointResponse>> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        // Generate a cache key based on pagination and the active-only filter.
        string key = DeliveryCacheConstants.GetPickupPointListKey(
            pageNumber,
            pageSize,
            activeOnly);

        // Return the cached result or load and cache the requested pickup point page.
        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                // Retrieve the requested page of pickup points.
                var result = await _queryRepository.GetPickupPointsAsync(
                    pageNumber,
                    pageSize,
                    activeOnly,
                    token);

                // Convert pickup point entities into response DTOs.
                var items = result.Items
                    .Select(MapPickupPoint)
                    .ToList();

                // Construct the paginated response.
                return CreatePagedResponse(
                    items,
                    pageNumber,
                    pageSize,
                    result.TotalCount);
            },
            options: CacheOptions,
            // Tag the entry so all pickup point lists can be invalidated together.
            tags: new[]
            {
                DeliveryCacheConstants.PickupPointListsTag
            },
            cancellationToken: cancellationToken);
    }

    // Retrieves an individual pickup point from the cache or repository.
    public async Task<PickupPointResponse?> GetPickupPointByIdAsync(
        int pickupPointId,
        CancellationToken cancellationToken)
    {
        // Generate the cache key for the specified pickup point.
        string key =
            DeliveryCacheConstants.GetPickupPointDetailsKey(pickupPointId);

        // Return the cached pickup point or retrieve it from the repository on a cache miss.
        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                // Load the pickup point by its ID.
                PickupPoint? point =
                    await _pickupPointRepository.GetByIdAsync(
                        pickupPointId,
                        token);

                // Return null if the pickup point does not exist; otherwise, map it to a DTO.
                return point is null ? null : MapPickupPoint(point);
            },
            options: CacheOptions,
            // Associate the entry with a tag for pickup point-specific invalidation.
            tags: new[]
            {
                DeliveryCacheConstants.GetPickupPointTag(pickupPointId)
            },
            cancellationToken: cancellationToken);
    }

    // Invalidates the specified delivery city's details and all cached city lists.
    public async Task InvalidateCityAsync(
        int cityId,
        CancellationToken cancellationToken)
    {
        // Remove cache entries associated with the specified delivery city.
        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.GetDeliveryCityTag(cityId),
            cancellationToken);

        // Invalidate city lists because the city's data may affect their results.
        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.DeliveryCityListsTag,
            cancellationToken);
    }

    // Invalidates the specified pickup point's details and all cached pickup point lists.
    public async Task InvalidatePickupPointAsync(
        int pickupPointId,
        CancellationToken cancellationToken)
    {
        // Remove cache entries associated with the specified pickup point.
        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.GetPickupPointTag(pickupPointId),
            cancellationToken);

        // Invalidate pickup point lists because the pickup point's data may affect their results.
        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.PickupPointListsTag,
            cancellationToken);
    }

    // Invalidates all cached delivery city and pickup point lists.
    public async Task InvalidateListsAsync(
        CancellationToken cancellationToken)
    {
        // Remove all cached delivery city list entries.
        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.DeliveryCityListsTag,
            cancellationToken);

        // Remove all cached pickup point list entries.
        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.PickupPointListsTag,
            cancellationToken);
    }

    // Maps a delivery city entity to its API response DTO.
    private static DeliveryCityResponse MapCity(DeliveryCity city)
    {
        return new DeliveryCityResponse
        {
            Id = city.Id,
            Name = city.Name,
            DeliveryFee = city.DeliveryFee,
            IsActive = city.IsActive,
            CreatedAt = city.CreatedAt,
            UpdatedAt = city.UpdatedAt
        };
    }

    // Maps a pickup point entity to its API response DTO.
    private static PickupPointResponse MapPickupPoint(PickupPoint point)
    {
        return new PickupPointResponse
        {
            Id = point.Id,
            Name = point.Name,
            Address = point.Address,
            PickupFee = point.PickupFee,
            IsActive = point.IsActive,
            CreatedAt = point.CreatedAt,
            UpdatedAt = point.UpdatedAt
        };
    }

    // Creates a paginated response containing the items and pagination metadata.
    private static PagedResponse<T> CreatePagedResponse<T>(
        List<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        return new PagedResponse<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}