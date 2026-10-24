using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Delivery;

// Defines the contract for caching delivery cities, pickup points, and invalidating cached delivery settings.
public interface IDeliverySettingsCacheService
{
    // Retrieves a paginated list of delivery cities, optionally filtering for active cities only.
    Task<PagedResponse<DeliveryCityResponse>> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    // Retrieves a delivery city by its ID from the cache, if available.
    Task<DeliveryCityResponse?> GetCityByIdAsync(
        int cityId,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of pickup points, optionally filtering for active pickup points only.
    Task<PagedResponse<PickupPointResponse>> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    // Retrieves a pickup point by its ID from the cache, if available.
    Task<PickupPointResponse?> GetPickupPointByIdAsync(
        int pickupPointId,
        CancellationToken cancellationToken);

    // Invalidates the cached data for a specific delivery city.
    Task InvalidateCityAsync(
        int cityId,
        CancellationToken cancellationToken);

    // Invalidates the cached data for a specific pickup point.
    Task InvalidatePickupPointAsync(
        int pickupPointId,
        CancellationToken cancellationToken);

    // Invalidates cached lists of delivery cities and pickup points.
    Task InvalidateListsAsync(
        CancellationToken cancellationToken);
}