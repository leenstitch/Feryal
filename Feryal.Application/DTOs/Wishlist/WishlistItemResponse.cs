namespace Feryal.Application.DTOs.Wishlist;

public sealed class WishlistItemResponse
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? PrimaryImageUrl { get; set; }

    public int ProductVariantId { get; set; }

    public string? SKU { get; set; }

    public string? Description { get; set; }

    public int? ColorId { get; set; }

    public string? ColorName { get; set; }

    public string? ColorHexCode { get; set; }

    public int? SizeId { get; set; }

    public string? SizeName { get; set; }

    public int? ScentId { get; set; }

    public string? ScentName { get; set; }

    public decimal? Price { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal? FinalPrice { get; set; }

    public bool IsVariantActive { get; set; }

    public bool IsInStock { get; set; }
    public int? StockQuantity { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public bool isActive { get; set; }
}