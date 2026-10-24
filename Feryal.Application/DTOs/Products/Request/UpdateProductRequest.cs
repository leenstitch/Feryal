using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Feryal.Application.DTOs.Products.Request
{
    public sealed class UpdateProductRequest
    {
        [Range(1, int.MaxValue)]
        public int? CategoryId { get; init; }

        [Range(1, int.MaxValue)]
        public int? ProductGroupId { get; init; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public JsonElement Name { get; init; } 

        [StringLength(2000)]
        public JsonElement Description { get; init; }
    }

}
