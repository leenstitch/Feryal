using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Feryal.Application.DTOs.Products.Request
{
    // Represents the request used to create a new product color.
    public sealed class CreateProductColorRequest
    {
        // Gets or initializes the required color name, limited to 50 characters.
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;

        // Gets or initializes the optional hexadecimal color code, limited to 20 characters.
        [StringLength(20)]
        public string? HexCode { get; init; }
    }

}