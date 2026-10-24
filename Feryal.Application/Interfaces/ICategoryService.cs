using Feryal.Application.DTOs.Categories;
using Feryal.Application.DTOs.Common;

// Defines the contract for category-related business operations.
public interface ICategoryService
{
    // Retrieves a paginated list of categories.
    Task<PagedResponse<CategoryResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Retrieves a category by its ID.
    Task<CategoryResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    // Creates a new category using the provided request data.
    Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken);

    // Updates an existing category using the provided request data.
    Task<CategoryResponse> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken);

    // Deletes a category by its ID.
    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken);

    // Activates a category by its ID.
    Task<CategoryResponse> ActivateAsync(
        int id,
        CancellationToken cancellationToken);

    // Deactivates a category by its ID.
    Task<CategoryResponse> DeactivateAsync(
        int id,
        CancellationToken cancellationToken);
}