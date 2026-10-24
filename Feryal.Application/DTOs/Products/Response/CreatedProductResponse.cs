namespace Feryal.Application.DTOs.Products.Response;

public sealed class CreatedProductResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }
    public int CategoryId { get; init; }
    public int ProductGroupId { get; init; }
    public string ?Description { get; init; }
    public List<ProductColorResponse> Colors { get; init; } = [];

    public List<ProductSizeResponse> Sizes { get; init; } = [];

    public List<ProductScentResponse> Scents { get; init; } = [];

    public List<ProductVariantResponse> Variants { get; init; } = [];

}

