
using Feryal.Domain.Entities;

namespace Feryal.Application.Interfaces;

public interface IDeliverySettingsQueryRepository
{
    Task<(List<DeliveryCity> Items, int TotalCount)> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<(List<PickupPoint> Items, int TotalCount)> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<bool> CityNameExistsAsync(
        string name,
        int? excludeCityId,
        CancellationToken cancellationToken);

   
            Task<bool> pickuppointNameExistsAsync(
        string name,
        int? excludeCityId,
        CancellationToken cancellationToken);
}
