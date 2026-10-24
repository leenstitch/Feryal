using System;
using System.Collections.Generic;
using System.Text;

namespace Feryal.Application.DTOs.Products.Response
{
    public sealed class ProductVariantResponse
    {
        public int Id { get; init; }

        public int ProductId { get; init; }

        public int? ScentId { get; init; }

        public string? ScentName { get; init; }

        public int? SizeId { get; init; }

        public string? SizeName { get; init; }

        public int? ColorId { get; init; }

        public string? ColorName { get; init; }

        public string? ColorHexCode { get; init; }

        public string SKU { get; init; } = string.Empty;

        public string? Description { get; init; }

        public decimal? Price { get; init; }

        public int? StockQuantity { get; init; }

        public bool IsActive { get; init; }

        public bool IsConfigured { get; init; }

        public bool HasCompleteSellingDetails { get; init; }

        public bool IsInStock => StockQuantity.GetValueOrDefault() > 0;

     public decimal  ? DiscountPercentage { get; init; }
        public decimal ?DiscountAmount { get; init; }
        public decimal? FinalPrice { get; init; }
    }
}
