namespace Feryal.Application.DTOs.Products.Response;

// Represents the response returned after creating a product, including its options and variants.
public sealed class CreatedProductResponse
{
    // Gets the unique identifier of the created product.
    public int Id { get; init; }

    // Gets the name of the created product.
    public string Name { get; init; } = string.Empty;

    // Gets whether the product is active.
    public bool IsActive { get; init; }

    // Gets the ID of the category to which the product belongs.
    public int CategoryId { get; init; }

    // Gets the ID of the product group to which the product belongs.
    public int ProductGroupId { get; init; }

    // Gets the optional description of the product.
    public string? Description { get; init; }

    // Gets the list of colors associated with the product.
    public List<ProductColorResponse> Colors { get; init; } = [];

    // Gets the list of sizes associated with the product.
    public List<ProductSizeResponse> Sizes { get; init; } = [];

    // Gets the list of scents associated with the product.
    public List<ProductScentResponse> Scents { get; init; } = [];

    // Gets the list of variants associated with the product.
    public List<ProductVariantResponse> Variants { get; init; } = [];
}