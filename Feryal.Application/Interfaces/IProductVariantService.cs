using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;


namespace Feryal.Application.Products.Interfaces;

public interface IProductVariantService
{
    ValueTask<PagedResponse<ProductVariantResponse>?> GetVariantsAsync(
        int productId,
        ProductVariantQueryParameters parameters,
        CancellationToken cancellationToken);

    ValueTask<PagedResponse<ProductVariantResponse>?> GetAvailableVariantsAsync(
        int productId,
        ProductVariantQueryParameters parameters,
        CancellationToken cancellationToken);

    Task<ProductVariantResponse?> UpdateVariantAsync(
        int variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken);

    Task<bool> ActivateAsync(
        int variantId,
        CancellationToken cancellationToken);

    Task<bool> DeactivateAsync(
        int variantId,
        CancellationToken cancellationToken);

    Task<bool> ShowAsync(
        int variantId,
        CancellationToken cancellationToken);

    Task<bool> HideAsync(
        int variantId,
        CancellationToken cancellationToken);
}