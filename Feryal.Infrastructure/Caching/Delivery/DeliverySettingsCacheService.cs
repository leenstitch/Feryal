
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Delivery;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Caching.Delivery;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Delivery;

public sealed class DeliverySettingsCacheService
    : IDeliverySettingsCacheService
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    private readonly HybridCache _cache;
    private readonly IDeliverySettingsQueryRepository _queryRepository;
    private readonly IRepository<DeliveryCity> _cityRepository;
    private readonly IRepository<PickupPoint> _pickupPointRepository;

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

    public async Task<PagedResponse<DeliveryCityResponse>> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        string key = DeliveryCacheConstants.GetDeliveryCityListKey(
            pageNumber,
            pageSize,
            activeOnly);

        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                var result = await _queryRepository.GetCitiesAsync(
                    pageNumber,
                    pageSize,
                    activeOnly,
                    token);

                var items = result.Items
                    .Select(MapCity)
                    .ToList();

                return CreatePagedResponse(
                    items,
                    pageNumber,
                    pageSize,
                    result.TotalCount);
            },
            options: CacheOptions,
            tags: new[]
            {
                DeliveryCacheConstants.DeliveryCityListsTag
            },
            cancellationToken: cancellationToken);
    }

    public async Task<DeliveryCityResponse?> GetCityByIdAsync(
        int cityId,
        CancellationToken cancellationToken)
    {
        string key =
            DeliveryCacheConstants.GetDeliveryCityDetailsKey(cityId);

        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                DeliveryCity? city =
                    await _cityRepository.GetByIdAsync(cityId, token);

                return city is null ? null : MapCity(city);
            },
            options: CacheOptions,
            tags: new[]
            {
                DeliveryCacheConstants.GetDeliveryCityTag(cityId)
            },
            cancellationToken: cancellationToken);
    }

    public async Task<PagedResponse<PickupPointResponse>> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        string key = DeliveryCacheConstants.GetPickupPointListKey(
            pageNumber,
            pageSize,
            activeOnly);

        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                var result = await _queryRepository.GetPickupPointsAsync(
                    pageNumber,
                    pageSize,
                    activeOnly,
                    token);

                var items = result.Items
                    .Select(MapPickupPoint)
                    .ToList();

                return CreatePagedResponse(
                    items,
                    pageNumber,
                    pageSize,
                    result.TotalCount);
            },
            options: CacheOptions,
            tags: new[]
            {
                DeliveryCacheConstants.PickupPointListsTag
            },
            cancellationToken: cancellationToken);
    }

    public async Task<PickupPointResponse?> GetPickupPointByIdAsync(
        int pickupPointId,
        CancellationToken cancellationToken)
    {
        string key =
            DeliveryCacheConstants.GetPickupPointDetailsKey(pickupPointId);

        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                PickupPoint? point =
                    await _pickupPointRepository.GetByIdAsync(
                        pickupPointId,
                        token);

                return point is null ? null : MapPickupPoint(point);
            },
            options: CacheOptions,
            tags: new[]
            {
                DeliveryCacheConstants.GetPickupPointTag(pickupPointId)
            },
            cancellationToken: cancellationToken);
    }

    public async Task InvalidateCityAsync(
        int cityId,
        CancellationToken cancellationToken)
    {
        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.GetDeliveryCityTag(cityId),
            cancellationToken);

        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.DeliveryCityListsTag,
            cancellationToken);
    }

    public async Task InvalidatePickupPointAsync(
        int pickupPointId,
        CancellationToken cancellationToken)
    {
        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.GetPickupPointTag(pickupPointId),
            cancellationToken);

        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.PickupPointListsTag,
            cancellationToken);
    }

    public async Task InvalidateListsAsync(
        CancellationToken cancellationToken)
    {
        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.DeliveryCityListsTag,
            cancellationToken);

        await _cache.RemoveByTagAsync(
            DeliveryCacheConstants.PickupPointListsTag,
            cancellationToken);
    }

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
