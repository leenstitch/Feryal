
using Feryal.Domain.Entities;

namespace Feryal.Application.Interfaces;

public interface IProductGroupRepository
    : IRepository<ProductGroup>
{
    Task<(List<ProductGroup> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        int? excludeProductGroupId,
        CancellationToken cancellationToken);

    Task<bool> HasProductsAsync(
        int productGroupId,
        CancellationToken cancellationToken);
}