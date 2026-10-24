
using Feryal.Application.DTOs.Products;
using Feryal.Domain.Entities;

namespace Feryal.Application.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
         ProductQueryParameters parameters,
         CancellationToken cancellationToken);

    Task<Product?> GetByIdWithDetailsAsync(
        int productId,
        CancellationToken cancellationToken);
    Task<(List<Product> Items, int TotalCount)> GetPagedActiveAsync(
    ProductQueryParameters parameters,
    CancellationToken cancellationToken);
    Task<Product?> GetByIdWithDetailsForUpdateAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        int? excludeProductId,
        CancellationToken cancellationToken);

    Task<bool> IsCategoryAvailableAsync(
        int categoryId,
        CancellationToken cancellationToken);

    Task<bool> IsProductGroupAvailableAsync(
        int productGroupId,
        CancellationToken cancellationToken);

    Task<List<ProductVariant>?> GetVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<List<ProductVariant>?> GetVisibleVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
        GetPagedVariantsByProductIdAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken);

    Task<(bool ProductExists, List<ProductVariant> Items, int TotalCount)>
        GetPagedAvailableVariantsByProductIdAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken);

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