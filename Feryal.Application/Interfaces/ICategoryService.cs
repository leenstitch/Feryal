using Feryal.Application.DTOs.Categories;
using Feryal.Application.DTOs.Common;

namespace Feryal.Application.Interfaces;

public interface ICategoryService
{
    Task<PagedResponse<CategoryResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CategoryResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken);

    Task<CategoryResponse> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken);

    Task<CategoryResponse> ActivateAsync(
    int id,
    CancellationToken cancellationToken);

    Task<CategoryResponse> DeactivateAsync(
        int id,
        CancellationToken cancellationToken);
}