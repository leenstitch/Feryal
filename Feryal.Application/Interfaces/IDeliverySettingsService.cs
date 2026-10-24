using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Delivery;

// Defines the contract for managing delivery cities and pickup points.
public interface IDeliverySettingsService
{
    // Creates a new delivery city using the provided request data.
    Task<DeliveryCityResponse> CreateCityAsync(
        CreateDeliveryCityRequest request,
        CancellationToken cancellationToken);

    // Partially updates an existing delivery city and returns the updated city, if found.
    Task<DeliveryCityResponse?> PatchCityAsync(
        int id,
        PatchDeliveryCityRequest request,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of delivery cities, optionally filtering for active cities only.
    Task<PagedResponse<DeliveryCityResponse>> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    // Retrieves a delivery city by its ID, if found.
    Task<DeliveryCityResponse?> GetCityByIdAsync(
        int id,
        CancellationToken cancellationToken);

    // Creates a new pickup point using the provided request data.
    Task<PickupPointResponse> CreatePickupPointAsync(
        CreatePickupPointRequest request,
        CancellationToken cancellationToken);

    // Partially updates an existing pickup point and returns the updated pickup point, if found.
    Task<PickupPointResponse?> PatchPickupPointAsync(
        int id,
        PatchPickupPointRequest request,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of pickup points, optionally filtering for active pickup points only.
    Task<PagedResponse<PickupPointResponse>> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    // Retrieves a pickup point by its ID, if found.
    Task<PickupPointResponse?> GetPickupPointByIdAsync(
        int id,
        CancellationToken cancellationToken);
}