using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Feryal.Application.DTOs.Products.Request
{
    // Represents the request used to update a product's details.
    public sealed class UpdateProductRequest
    {
        // Gets or initializes the optional category ID; if provided, it must be a positive integer.
        [Range(1, int.MaxValue)]
        public int? CategoryId { get; init; }

        // Gets or initializes the optional product group ID; if provided, it must be a positive integer.
        [Range(1, int.MaxValue)]
        public int? ProductGroupId { get; init; }

        // Gets or initializes the product name as a JSON element to support custom update handling.
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public JsonElement Name { get; init; }

        // Gets or initializes the product description as a JSON element to support custom update handling.
        [StringLength(2000)]
        public JsonElement Description { get; init; }
    }

}