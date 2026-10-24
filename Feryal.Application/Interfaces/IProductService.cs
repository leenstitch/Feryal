using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;

namespace Feryal.Application.Interfaces;

// Defines the contract for product and product variant business operations.
public interface IProductService
{
    // Retrieves a paginated list of products based on the specified query parameters.
    Task<PagedResponse<ProductResponse>> GetPagedProductsAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken);

    // Retrieves a product by its ID, including its details.
    Task<ProductResponse?> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of active products based on the specified query parameters.
    Task<PagedResponse<ProductResponse>> GetPagedActiveProductsAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken);

    // Creates a new product using the provided request data.
    Task<CreatedProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);

    // Updates an existing product using the provided request data.
    Task<ProductResponse?> UpdateAsync(
        int productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken);

    // Partially updates an existing product using the provided request data.
    Task<ProductResponse?> PatchAsync(
        int productId,
        PatchProductRequest request,
        CancellationToken cancellationToken);

    // Adds a color option to the specified product and returns the created option with its variants.
    Task<AddedProductOptionResponse?> AddColorAsync(
        int productId,
        CreateProductColorRequest request,
        CancellationToken cancellationToken);

    // Adds a size option to the specified product and returns the created option with its variants.
    Task<AddedProductOptionResponse?> AddSizeAsync(
        int productId,
        CreateProductSizeRequest request,
        CancellationToken cancellationToken);

    // Adds a scent option to the specified product and returns the created option with its variants.
    Task<AddedProductOptionResponse?> AddScentAsync(
        int productId,
        CreateProductScentRequest request,
        CancellationToken cancellationToken);

    // Activates the specified product.
    Task<bool> ActivateAsync(
        int productId,
        CancellationToken cancellationToken);

    // Deactivates the specified product.
    Task<bool> DeactivateAsync(
        int productId,
        CancellationToken cancellationToken);

    // Updates the details of a product variant.
    Task<ProductVariantResponse?> UpdateVariantAsync(
        int variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken);

    // Activates the specified product variant.
    Task<bool> ActivateVariantAsync(
        int variantId,
        CancellationToken cancellationToken);

    // Deactivates the specified product variant.
    Task<bool> DeactivateVariantAsync(
        int variantId,
        CancellationToken cancellationToken);

    // Retrieves all variants associated with the specified product.
    Task<List<ProductVariantResponse>?> GetVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken);

    // Makes the specified product variant visible.
    Task<bool> ShowVariantAsync(
        int variantId,
        CancellationToken cancellationToken);

    // Hides the specified product variant.
    Task<bool> HideVariantAsync(
        int variantId,
        CancellationToken cancellationToken);

    // Retrieves the visible variants associated with the specified product.
    Task<List<ProductVariantResponse>?> GetVisibleVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken);
}