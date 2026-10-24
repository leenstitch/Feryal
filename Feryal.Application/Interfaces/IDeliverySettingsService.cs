
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Delivery;

namespace Feryal.Application.Interfaces;

public interface IDeliverySettingsService
{
    Task<DeliveryCityResponse> CreateCityAsync(
        CreateDeliveryCityRequest request,
        CancellationToken cancellationToken);

    Task<DeliveryCityResponse?> PatchCityAsync(
        int id,
        PatchDeliveryCityRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<DeliveryCityResponse>> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<DeliveryCityResponse?> GetCityByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<PickupPointResponse> CreatePickupPointAsync(
        CreatePickupPointRequest request,
        CancellationToken cancellationToken);

    Task<PickupPointResponse?> PatchPickupPointAsync(
        int id,
        PatchPickupPointRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<PickupPointResponse>> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<PickupPointResponse?> GetPickupPointByIdAsync(
        int id,
        CancellationToken cancellationToken);
}
