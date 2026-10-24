using System;
using System.Collections.Generic;
using System.Text;

namespace Feryal.Application.DTOs.Products.Response
{
    // Represents the response returned after adding a product option and creating its associated variants.
    public sealed class AddedProductOptionResponse
    {
        // Gets the unique identifier of the added product option.
        public int Id { get; init; }

        // Gets the ID of the product to which the option belongs.
        public int ProductId { get; init; }

        // Gets the name of the added product option.
        public string Name { get; init; } = string.Empty;

        // Gets the hexadecimal color code of the option, if applicable.
        public string? HexCode { get; init; }

        // Gets the list of product variants created for the added option.
        public List<ProductVariantResponse> CreatedVariants { get; init; } = [];
    }
}