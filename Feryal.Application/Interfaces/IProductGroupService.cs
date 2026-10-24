
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.ProductGroups;

namespace Feryal.Application.Interfaces;

public interface IProductGroupService
{
    Task<PagedResponse<ProductGroupResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ProductGroupResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<ProductGroupResponse> CreateAsync(
        CreateProductGroupRequest request,
        CancellationToken cancellationToken);

    Task<ProductGroupResponse> UpdateAsync(
        int id,
        UpdateProductGroupRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken);

    Task<ProductGroupResponse> ActivateAsync(
        int id,
        CancellationToken cancellationToken);

    Task<ProductGroupResponse> DeactivateAsync(
        int id,
        CancellationToken cancellationToken);
}