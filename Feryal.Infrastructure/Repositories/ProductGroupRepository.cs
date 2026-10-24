using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Provides database operations for product groups using the generic repository.
public class ProductGroupRepository
    : Repository<ProductGroup>, IProductGroupRepository
{
    private readonly ApplicationDbContext _dbContext;

    // Inject the database context and initialize the base repository.
    public ProductGroupRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // Retrieves a paginated list of product groups and the total number of records.
    public async Task<(List<ProductGroup> Items, int TotalCount)>
        GetPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        // Build a read-only query without tracking entities.
        var query = _dbContext.ProductGroups
            .AsNoTracking();

        // Count all product groups before applying pagination.
        var totalCount = await query.CountAsync(
            cancellationToken);

        // Retrieve the requested page ordered by name and then by ID.
        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Return the requested page and the total count.
        return (items, totalCount);
    }

    // Checks whether a product group with the specified name already exists.
    // Optionally excludes a product group ID when checking during an update.
    public async Task<bool> ExistsByNameAsync(
        string name,
        int? excludeProductGroupId,
        CancellationToken cancellationToken)
    {
        // Normalize the name by trimming whitespace and converting it to uppercase.
        var normalizedName = name.Trim().ToUpper();

        // Build a read-only query matching the normalized product group name.
        var query = _dbContext.ProductGroups
            .AsNoTracking()
            .Where(x => x.Name.Trim().ToUpper() == normalizedName);

        // Exclude the current product group when an ID is provided.
        if (excludeProductGroupId.HasValue)
        {
            query = query.Where(
                x => x.Id != excludeProductGroupId.Value);
        }

        // Return true if a matching product group exists.
        return await query.AnyAsync(cancellationToken);
    }

    // Checks whether any products belong to the specified product group.
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