using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;

// Defines the contract for category-specific database operations.
public interface ICategoryRepository : IRepository<Category>
{
    // Retrieves a paginated list of categories along with the total number of categories.
    Task<(List<Category> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Checks whether a category with the specified name exists, optionally excluding a category by ID.
    Task<bool> ExistsByNameAsync(
        string name,
        int? excludeCategoryId,
        CancellationToken cancellationToken);

    // Checks whether the specified category has any associated products.
    Task<bool> HasProductsAsync(
        int categoryId,
        CancellationToken cancellationToken);
}