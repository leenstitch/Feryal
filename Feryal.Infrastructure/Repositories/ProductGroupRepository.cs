
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public class ProductGroupRepository
    : Repository<ProductGroup>, IProductGroupRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProductGroupRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(List<ProductGroup> Items, int TotalCount)>
        GetPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        var query = _dbContext.ProductGroups
            .AsNoTracking();

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        int? excludeProductGroupId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToUpper();

        var query = _dbContext.ProductGroups
            .AsNoTracking()
            .Where(x => x.Name.Trim().ToUpper() == normalizedName);

        if (excludeProductGroupId.HasValue)
        {
            query = query.Where(
                x => x.Id != excludeProductGroupId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasProductsAsync(
        int productGroupId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AnyAsync(
                x => x.ProductGroupId == productGroupId,
                cancellationToken);
    }
}