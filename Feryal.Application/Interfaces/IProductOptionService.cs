using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;


namespace Feryal.Application.Products.Interfaces;

public interface IProductOptionService
{
    Task<AddedProductOptionResponse?> AddColorAsync(
        int productId,
        CreateProductColorRequest request,
        CancellationToken cancellationToken);

    Task<AddedProductOptionResponse?> AddSizeAsync(
        int productId,
        CreateProductSizeRequest request,
        CancellationToken cancellationToken);

    Task<AddedProductOptionResponse?> AddScentAsync(
        int productId,
        CreateProductScentRequest request,
        CancellationToken cancellationToken);
}