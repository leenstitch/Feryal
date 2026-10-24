using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Feryal.Application.DTOs.Products.Request
{
    // Represents the request used to create a new product size.
    public sealed class CreateProductSizeRequest
    {
        // Gets or initializes the required size name, limited to 50 characters.
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;
    }
}