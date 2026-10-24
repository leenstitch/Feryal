using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public class CategoryRepository
    : Repository<Category>, ICategoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CategoryRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(List<Category> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Categories
            .AsNoTracking();

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        int? excludeCategoryId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToUpper();

        return await _dbContext.Categories
            .AnyAsync(
                category =>
                    category.Name.Trim().ToUpper() == normalizedName &&
                    (!excludeCategoryId.HasValue ||
                     category.Id != excludeCategoryId.Value),
                cancellationToken);
    }

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