
using Feryal.Application.DTOs.Categories;
using Feryal.Application.DTOs.Common;

namespace Feryal.Application.Interfaces;

public interface ICategoryCacheService
{
    ValueTask<PagedResponse<CategoryResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    ValueTask<CategoryResponse?> GetByIdAsync(
        int categoryId,
        CancellationToken cancellationToken);

    ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken);

    ValueTask InvalidateCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken);
}