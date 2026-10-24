using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Feryal.Application.DTOs.Products.Request
{
    // Represents a request to update a product variant's details.
    public sealed class UpdateProductVariantRequest
    {
        // Gets or initializes the description value provided for the update, allowing detection of its JSON state.
        public JsonElement Description { get; init; }

        // Gets or initializes the price value provided for the update, allowing detection of its JSON state.
        public JsonElement Price { get; init; }

        // Gets or initializes the stock quantity value provided for the update, allowing detection of its JSON state.
        public JsonElement StockQuantity { get; init; }

        // Gets or initializes the discount percentage value provided for the update, allowing detection of its JSON state.
        public JsonElement DiscountPercentage { get; init; }
    }
}