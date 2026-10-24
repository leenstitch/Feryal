
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Response;

namespace Feryal.Application.Interfaces;

public interface IProductCacheService
{
    ValueTask<PagedResponse<ProductListResponse>> GetAllAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken);

    ValueTask<ProductResponse?> GetByIdAsync(
        int productId,
        CancellationToken cancellationToken);

    ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken);

    ValueTask InvalidateProductAsync(
        int productId,
        CancellationToken cancellationToken);
}
