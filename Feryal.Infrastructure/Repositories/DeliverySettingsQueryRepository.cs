
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public sealed class DeliverySettingsQueryRepository
    : IDeliverySettingsQueryRepository
{
    private readonly ApplicationDbContext _context;

    public DeliverySettingsQueryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(List<DeliveryCity> Items, int TotalCount)> GetCitiesAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        IQueryable<DeliveryCity> query =
            _context.DeliveryCities.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(city => city.IsActive);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<DeliveryCity> items = await query
            .OrderBy(city => city.Name)
            .ThenBy(city => city.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<PickupPoint> Items, int TotalCount)> GetPickupPointsAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        IQueryable<PickupPoint> query =
            _context.PickupPoints.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(point => point.IsActive);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<PickupPoint> items = await query
            .OrderBy(point => point.Name)
            .ThenBy(point => point.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> CityNameExistsAsync(
        string name,
        int? excludeCityId,
        CancellationToken cancellationToken)
    {
        IQueryable<DeliveryCity> query = _context.DeliveryCities
            .AsNoTracking()
            .Where(city => city.Name == name);

        if (excludeCityId.HasValue)
        {
            query = query.Where(
                city => city.Id != excludeCityId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> pickuppointNameExistsAsync(
      string name,
      int? excludeCityId,
      CancellationToken cancellationToken)
    {
        IQueryable<PickupPoint> query = _context.PickupPoints
            .AsNoTracking()
            .Where(PickupPoint => PickupPoint.Name == name);

        if (excludeCityId.HasValue)
        {
            query = query.Where(
                PickupPoint => PickupPoint.Id != excludeCityId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
