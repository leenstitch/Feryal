namespace Feryal.Application.DTOs.Wishlist;

// Represents a wishlist item with product details, variant information, pricing, and stock status.
public sealed class WishlistItemResponse
{
    // Gets or sets the unique identifier of the wishlist item.
    public int Id { get; set; }

    // Gets or sets the ID of the associated product.
    public int ProductId { get; set; }

    // Gets or sets the name of the product.
    public string ProductName { get; set; } = string.Empty;

    // Gets or sets the URL of the product's primary image.
    public string? PrimaryImageUrl { get; set; }

    // Gets or sets the ID of the selected product variant.
    public int ProductVariantId { get; set; }

    // Gets or sets the stock keeping unit (SKU) of the product variant.
    public string? SKU { get; set; }

    // Gets or sets the description of the product variant.
    public string? Description { get; set; }

    // Gets or sets the ID of the variant's color, if applicable.
    public int? ColorId { get; set; }

    // Gets or sets the name of the variant's color, if applicable.
    public string? ColorName { get; set; }

    // Gets or sets the hexadecimal code of the variant's color, if applicable.
    public string? ColorHexCode { get; set; }

    // Gets or sets the ID of the variant's size, if applicable.
    public int? SizeId { get; set; }

    // Gets or sets the name of the variant's size, if applicable.
    public string? SizeName { get; set; }

    // Gets or sets the ID of the variant's scent, if applicable.
    public int? ScentId { get; set; }

    // Gets or sets the name of the variant's scent, if applicable.
    public string? ScentName { get; set; }

    // Gets or sets the original price of the product variant.
    public decimal? Price { get; set; }

    // Gets or sets the discount percentage applied to the product variant.
    public decimal DiscountPercentage { get; set; }

    // Gets or sets the final price after applying the discount.
    public decimal? FinalPrice { get; set; }

    // Gets or sets whether the product variant is active.
    public bool IsVariantActive { get; set; }

    // Gets or sets whether the product variant is currently in stock.
    public bool IsInStock { get; set; }

    // Gets or sets the available stock quantity of the product variant.
    public int? StockQuantity { get; set; }

    // Gets or sets the date and time when the item was added to the wishlist.
    public DateTimeOffset AddedAt { get; set; }

    // Gets or sets whether the product is active.
    public bool isActive { get; set; }
}