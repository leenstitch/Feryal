namespace Feryal.Application.DTOs.Cart;

public sealed class CartItemResponse
{
    // Gets or sets the unique identifier of the cart item.
    public int Id { get; set; }

    // Gets or sets the ID of the product associated with the cart item.
    public int ProductId { get; set; }

    // Gets or sets the name of the product.
    public string ProductName { get; set; } = string.Empty;

    // Gets or sets the URL of the product's primary image.
    public string? PrimaryImageUrl { get; set; }

    // Gets or sets the ID of the selected product variant.
    public int ProductVariantId { get; set; }

    // Gets or sets the stock keeping unit (SKU) of the product variant.
    public string SKU { get; set; } = string.Empty;

    // Gets or sets the description of the product variant.
    public string? Description { get; set; }

    // Gets or sets the ID of the selected color.
    public int? ColorId { get; set; }

    // Gets or sets the name of the selected color.
    public string? ColorName { get; set; }

    // Gets or sets the hexadecimal color code of the selected color.
    public string? ColorHexCode { get; set; }

    // Gets or sets the ID of the selected size.
    public int? SizeId { get; set; }

    // Gets or sets the name of the selected size.
    public string? SizeName { get; set; }

    // Gets or sets the ID of the selected scent.
    public int? ScentId { get; set; }

    // Gets or sets the name of the selected scent.
    public string? ScentName { get; set; }

    // Gets or sets the original price of the product variant.
    public decimal? Price { get; set; }

    // Gets or sets the discount percentage applied to the product variant.
    public decimal DiscountPercentage { get; set; }

    // Gets or sets the final price of the product variant after applying the discount.
    public decimal? FinalPrice { get; set; }

    // Gets or sets the quantity of the product variant in the cart.
    public int Quantity { get; set; }

    // Gets or sets the available stock quantity of the product variant.
    public int? StockQuantity { get; set; }

    // Gets or sets the total price for this cart item based on its quantity and final price.
    public decimal? LineTotal { get; set; }

    // Gets or sets a value indicating whether the product is active.
    public bool IsProductActive { get; set; }

    // Gets or sets a value indicating whether the product variant is active.
    public bool IsVariantActive { get; set; }

    // Gets or sets a value indicating whether the product variant has been fully configured.
    public bool IsConfigured { get; set; }

    // Gets or sets a value indicating whether the product variant is in stock.
    public bool IsInStock { get; set; }
}