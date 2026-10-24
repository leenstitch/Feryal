using System;
using System.Collections.Generic;
using System.Text;
using Feryal.Application.Products.Images;
namespace Feryal.Application.DTOs.Products.Response
{
    public sealed class ProductResponse
    {
        public int Id { get; init; }

        public int CategoryId { get; init; }

        public int ProductGroupId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public IReadOnlyList<ProductColorResponse> Colors { get; init; } = [];

        public IReadOnlyList<ProductSizeResponse> Sizes { get; init; } = [];

        public IReadOnlyList<ProductScentResponse> Scents { get; init; } = [];

        public IReadOnlyList<ProductVariantResponse> Variants { get; init; } = [];

        public IReadOnlyList<ProductImageResponse> Images { get; init; } = [];
    }
}
