namespace Feryal.Application.Products.Images;

public sealed class ProductImageResponse
{
    public int Id { get; init; }

    public int ProductId { get; init; }

    public string ImageUrl { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    public int DisplayOrder { get; init; }
}