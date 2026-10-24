using Feryal.Domain.Entities;

namespace Feryal.Application.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<(List<Category> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        int? excludeCategoryId,
        CancellationToken cancellationToken);

    Task<bool> HasProductsAsync(
        int categoryId,
        CancellationToken cancellationToken);
}