using System;
using System.Collections.Generic;
using System.Text;

namespace Feryal.Application.DTOs.Products.Response
{
    public sealed class AddedProductOptionResponse
    {
        public int Id { get; init; }

        public int ProductId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? HexCode { get; init; }
        public List<ProductVariantResponse> CreatedVariants { get; init; } = [];
    }
}
