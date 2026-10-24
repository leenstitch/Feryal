using Feryal.Domain.Entities;

// Defines the contract for querying delivery cities and pickup points from the database.
public interface IDeliverySettingsQueryRepository
{
    // Retrieves a paginated list of delivery cities, optionally filtering for active cities only.
    Task<(List<DeliveryCity> Items, int TotalCount)> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of pickup points, optionally filtering for active pickup points only.
    Task<(List<PickupPoint> Items, int TotalCount)> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    // Checks whether a delivery city with the specified name exists, optionally excluding a city by ID.
    Task<bool> CityNameExistsAsync(
        string name,
        int? excludeCityId,
        CancellationToken cancellationToken);

    // Checks whether a pickup point with the specified name exists, optionally excluding a city by ID.
    Task<bool> pickuppointNameExistsAsync(
        string name,
        int? excludeCityId,
        CancellationToken cancellationToken);
}