using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;


namespace Feryal.Application.Products.Interfaces;

// Defines the contract for retrieving and managing product variants.
public interface IProductVariantService
{
    // Retrieves a paginated list of variants for the specified product.
    ValueTask<PagedResponse<ProductVariantResponse>?> GetVariantsAsync(
        int productId,
        ProductVariantQueryParameters parameters,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of available variants for the specified product.
    ValueTask<PagedResponse<ProductVariantResponse>?> GetAvailableVariantsAsync(
        int productId,
        ProductVariantQueryParameters parameters,
        CancellationToken cancellationToken);

    // Updates the details of an existing product variant.
    Task<ProductVariantResponse?> UpdateVariantAsync(
        int variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken);

    // Activates the specified product variant.
    Task<bool> ActivateAsync(
        int variantId,
        CancellationToken cancellationToken);

    // Deactivates the specified product variant.
    Task<bool> DeactivateAsync(
        int variantId,
        CancellationToken cancellationToken);

    // Makes the specified product variant visible.
    Task<bool> ShowAsync(
        int variantId,
        CancellationToken cancellationToken);

    // Hides the specified product variant.
    Task<bool> HideAsync(
        int variantId,
        CancellationToken cancellationToken);
}