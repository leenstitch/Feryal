using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;

// Defines the contract for product group-specific database operations.
public interface IProductGroupRepository
    : IRepository<ProductGroup>
{
    // Retrieves a paginated list of product groups along with the total number of product groups.
    Task<(List<ProductGroup> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Checks whether a product group with the specified name exists, optionally excluding a product group by ID.
    Task<bool> ExistsByNameAsync(
        string name,
        int? excludeProductGroupId,
        CancellationToken cancellationToken);

    // Checks whether the specified product group has any associated products.
    Task<bool> HasProductsAsync(
        int productGroupId,
        CancellationToken cancellationToken);
}