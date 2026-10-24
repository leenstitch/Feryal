using Feryal.Application.DTOs.Products;
using Feryal.Domain.Entities;

namespace Feryal.Application.Interfaces;

// Defines the contract for product and product variant database operations.
public interface IProductRepository : IRepository<Product>
{
    // Retrieves a paginated list of products based on the specified query parameters.
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
         ProductQueryParameters parameters,
         CancellationToken cancellationToken);

    // Retrieves a product by its ID, including its related details.
    Task<Product?> GetByIdWithDetailsAsync(
        int productId,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of active products based on the specified query parameters.
    Task<(List<Product> Items, int TotalCount)> GetPagedActiveAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken);

    // Retrieves a product by its ID with related details for update operations.
    Task<Product?> GetByIdWithDetailsForUpdateAsync(
        int productId,
        CancellationToken cancellationToken);

    // Checks whether a product with the specified name exists, optionally excluding a product by ID.
    Task<bool> ExistsByNameAsync(
        string name,
        int? excludeProductId,
        CancellationToken cancellationToken);

    // Checks whether the specified category is available for assigning to a product.
    Task<bool> IsCategoryAvailableAsync(
        int categoryId,
        CancellationToken cancellationToken);

    // Checks whether the specified product group is available for assigning to a product.
    Task<bool> IsProductGroupAvailableAsync(
        int productGroupId,
        CancellationToken cancellationToken);

    // Retrieves all variants associated with the specified product.
    Task<List<ProductVariant>?> GetVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken);

    // Retrieves the visible variants associated with the specified product.
    Task<List<ProductVariant>?> GetVisibleVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken);

    // Retrieves paginated variants for a product and indicates whether the product exists.
    Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
        GetPagedVariantsByProductIdAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken);

    // Retrieves paginated available variants for a product and indicates whether the product exists.
    Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
        GetPagedAvailableVariantsByProductIdAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken);

    // Retrieves a product variant by its ID for update operations.
    Task<ProductVariant?> GetVariantByIdForUpdateAsync(
        int variantId,
        CancellationToken cancellationToken);
}


//using Feryal.Application.DTOs.Products;

//using Feryal.Domain.Entities;

//namespace Feryal.Application.Interfaces;

//public interface IProductRepository : IRepository<Product>
//{
//    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
//        ProductQueryParameters parameters,
//        CancellationToken cancellationToken);

//    Task<Product?> GetByIdWithDetailsAsync(
//        int productId,
//        CancellationToken cancellationToken);

//    Task<Product?> GetByIdWithDetailsForUpdateAsync(
//        int productId,
//        CancellationToken cancellationToken);

//    Task<bool> ExistsByNameAsync(
//        string name,
//        int? excludeProductId,
//        CancellationToken cancellationToken);

//    Task<bool> IsCategoryAvailableAsync(
//        int categoryId,
//        CancellationToken cancellationToken);

//    Task<bool> IsProductGroupAvailableAsync(
//        int productGroupId,
//        CancellationToken cancellationToken);

//    Task<List<ProductVariant>?> GetVariantsByProductIdAsync(
//        int productId,
//        CancellationToken cancellationToken);

//    Task<List<ProductVariant>?> GetVisibleVariantsByProductIdAsync(
//        int productId,
//        CancellationToken cancellationToken);

//    Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
//        GetPagedVariantsByProductIdAsync(
//            int productId,
//            ProductVariantQueryParameters parameters,
//            CancellationToken cancellationToken);

//    Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
//        GetPagedAvailableVariantsByProductIdAsync(
//            int productId,
//            ProductVariantQueryParameters parameters,
//            CancellationToken cancellationToken);
//}