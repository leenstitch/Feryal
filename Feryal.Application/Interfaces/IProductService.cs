using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;

namespace Feryal.Application.Interfaces;

public interface IProductService
{


    Task<PagedResponse<ProductResponse>> GetPagedProductsAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken);

    Task<ProductResponse?> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<PagedResponse<ProductResponse>> GetPagedActiveProductsAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken);

    Task<CreatedProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);

    Task<ProductResponse?> UpdateAsync(
        int productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken);

    Task<ProductResponse?> PatchAsync(
        int productId,
        PatchProductRequest request,
        CancellationToken cancellationToken);

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

    Task<bool> ActivateAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<bool> DeactivateAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<ProductVariantResponse?> UpdateVariantAsync(
        int variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken);

    Task<bool> ActivateVariantAsync(
        int variantId,
        CancellationToken cancellationToken);

    Task<bool> DeactivateVariantAsync(
        int variantId,
        CancellationToken cancellationToken);

    Task<List<ProductVariantResponse>?> GetVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<bool> ShowVariantAsync(
        int variantId,
        CancellationToken cancellationToken);

    Task<bool> HideVariantAsync(
        int variantId,
        CancellationToken cancellationToken);

    Task<List<ProductVariantResponse>?> GetVisibleVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken);
}