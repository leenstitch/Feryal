using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Provides read-only queries for delivery cities and pickup points.
public sealed class DeliverySettingsQueryRepository
    : IDeliverySettingsQueryRepository
{
    private readonly ApplicationDbContext _context;

    // Inject the database context to access delivery settings.
    public DeliverySettingsQueryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Retrieves a paginated list of delivery cities with an optional active-only filter.
    public async Task<(List<DeliveryCity> Items, int TotalCount)> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        // Build a read-only query for delivery cities.
        IQueryable<DeliveryCity> query =
            _context.DeliveryCities.AsNoTracking();

        // Include only active cities when requested.
        if (activeOnly)
        {
            query = query.Where(city => city.IsActive);
        }

        // Count matching cities before applying pagination.
        int totalCount = await query.CountAsync(cancellationToken);

        // Retrieve the requested page in alphabetical order.
        List<DeliveryCity> items = await query
            .OrderBy(city => city.Name)
            .ThenBy(city => city.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Return the cities and the total number of matching records.
        return (items, totalCount);
    }

    // Retrieves a paginated list of pickup points with an optional active-only filter.
    public async Task<(List<PickupPoint> Items, int TotalCount)> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        // Build a read-only query for pickup points.
        IQueryable<PickupPoint> query =
            _context.PickupPoints.AsNoTracking();

        // Include only active pickup points when requested.
        if (activeOnly)
        {
            query = query.Where(point => point.IsActive);
        }

        // Count matching pickup points before applying pagination.
        int totalCount = await query.CountAsync(cancellationToken);

        // Retrieve the requested page in alphabetical order.
        List<PickupPoint> items = await query
            .OrderBy(point => point.Name)
            .ThenBy(point => point.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Return the pickup points and the total number of matching records.
        return (items, totalCount);
    }

    // Checks whether a delivery city with the specified name exists.
    // Optionally excludes a city ID when checking during an update.
    public async Task<bool> CityNameExistsAsync(
        string name,
        int? excludeCityId,
        CancellationToken cancellationToken)
    {
        // Build a query matching the specified city name.
        IQueryable<DeliveryCity> query = _context.DeliveryCities
            .AsNoTracking()
            .Where(city => city.Name == name);

        // Exclude the current city when an ID is provided.
        if (excludeCityId.HasValue)
        {
            query = query.Where(
                city => city.Id != excludeCityId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    // Checks whether a pickup point with the specified name exists.
    // Optionally excludes a pickup point ID when checking during an update.
    public async Task<bool> pickuppointNameExistsAsync(
      string name,
      int? excludeCityId,
      CancellationToken cancellationToken)
    {
        // Build a query matching the specified pickup point name.
        IQueryable<PickupPoint> query = _context.PickupPoints
            .AsNoTracking()
            .Where(PickupPoint => PickupPoint.Name == name);

        // Exclude the specified record when an ID is provided.
        if (excludeCityId.HasValue)
        {
            query = query.Where(
                PickupPoint => PickupPoint.Id != excludeCityId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}