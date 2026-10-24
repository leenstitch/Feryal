using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Feryal.Application.DTOs.Products.Request
{
    public sealed class CreateProductScentRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;
    }
}
