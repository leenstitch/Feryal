using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;


namespace Feryal.Application.Products.Interfaces;

// Defines the contract for adding colors, sizes, and scents to products.
public interface IProductOptionService
{
    // Adds a color to the specified product and returns the created option with its generated variants, if successful.
    Task<AddedProductOptionResponse?> AddColorAsync(
        int productId,
        CreateProductColorRequest request,
        CancellationToken cancellationToken);

    // Adds a size to the specified product and returns the created option with its generated variants, if successful.
    Task<AddedProductOptionResponse?> AddSizeAsync(
        int productId,
        CreateProductSizeRequest request,
        CancellationToken cancellationToken);

    // Adds a scent to the specified product and returns the created option with its generated variants, if successful.
    Task<AddedProductOptionResponse?> AddScentAsync(
        int productId,
        CreateProductScentRequest request,
        CancellationToken cancellationToken);
}