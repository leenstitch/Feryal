using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Feryal.Application.DTOs.Products.Request
{
    // Represents the request used to create a new product scent.
    public sealed class CreateProductScentRequest
    {
        // Gets or initializes the required scent name, limited to 100 characters.
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;
    }
}