using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Feryal.Application.DTOs.Products.Request
{
    public sealed class UpdateProductVariantRequest
    {
        
        public JsonElement Description { get; init; }

        
        public JsonElement Price { get; init; }

        
        public JsonElement StockQuantity { get; init; }

        
        public JsonElement DiscountPercentage { get; init; }
    }
}
