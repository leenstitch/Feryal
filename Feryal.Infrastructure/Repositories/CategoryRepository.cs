using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Provides database operations for categories using the generic repository.
public class CategoryRepository
    : Repository<Category>, ICategoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    // Inject the database context and initialize the base repository.
    public CategoryRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // Retrieves a paginated list of categories and the total number of categories.
    public async Task<(List<Category> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Build a read-only query without tracking entities.
        var query = _dbContext.Categories
            .AsNoTracking();

        // Count all categories before applying pagination.
        var totalCount = await query.CountAsync(
            cancellationToken);

        // Retrieve the requested page in alphabetical order, using the ID as a secondary sort.
        var items = await query
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Return the page of categories and the total count.
        return (items, totalCount);
    }

    // Checks whether a category with the same name already exists.
    // Optionally excludes a category ID when checking during an update.
    public async Task<bool> ExistsByNameAsync(
        string name,
        int? excludeCategoryId,
        CancellationToken cancellationToken)
    {
        // Normalize the name by trimming whitespace and converting it to uppercase.
        var normalizedName = name.Trim().ToUpper();

        return await _dbContext.Categories
            .AnyAsync(
                category =>
                    category.Name.Trim().ToUpper() == normalizedName &&
                    (!excludeCategoryId.HasValue ||
                     category.Id != excludeCategoryId.Value),
                cancellationToken);
    }

    // Checks whether any products are associated with the specified category.
    public async Task<bool> HasProductsAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AnyAsync(
                product => product.CategoryId == categoryId,
                cancellationToken);
    }
}