
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Delivery;

namespace Feryal.Application.Interfaces;

public interface IDeliverySettingsCacheService
{
    Task<PagedResponse<DeliveryCityResponse>> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<DeliveryCityResponse?> GetCityByIdAsync(
        int cityId,
        CancellationToken cancellationToken);

    Task<PagedResponse<PickupPointResponse>> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<PickupPointResponse?> GetPickupPointByIdAsync(
        int pickupPointId,
        CancellationToken cancellationToken);

    Task InvalidateCityAsync(
        int cityId,
        CancellationToken cancellationToken);

    Task InvalidatePickupPointAsync(
        int pickupPointId,
        CancellationToken cancellationToken);

    Task InvalidateListsAsync(
        CancellationToken cancellationToken);
}
