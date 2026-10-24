using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.ProductGroups;

// Defines the contract for product group-related business operations.
public interface IProductGroupService
{
    // Retrieves a paginated list of product groups.
    Task<PagedResponse<ProductGroupResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Retrieves a product group by its ID.
    Task<ProductGroupResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    // Creates a new product group using the provided request data.
    Task<ProductGroupResponse> CreateAsync(
        CreateProductGroupRequest request,
        CancellationToken cancellationToken);

    // Updates an existing product group using the provided request data.
    Task<ProductGroupResponse> UpdateAsync(
        int id,
        UpdateProductGroupRequest request,
        CancellationToken cancellationToken);

    // Deletes a product group by its ID.
    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken);

    // Activates a product group by its ID.
    Task<ProductGroupResponse> ActivateAsync(
        int id,
        CancellationToken cancellationToken);

    // Deactivates a product group by its ID.
    Task<ProductGroupResponse> DeactivateAsync(
        int id,
        CancellationToken cancellationToken);
}