
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.ProductGroups;

namespace Feryal.Application.Interfaces;

public interface IProductGroupCacheService
{
    ValueTask<PagedResponse<ProductGroupResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    ValueTask<ProductGroupResponse?> GetByIdAsync(
        int productGroupId,
        CancellationToken cancellationToken);

    ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken);

    ValueTask InvalidateProductGroupAsync(
        int productGroupId,
        CancellationToken cancellationToken);
}