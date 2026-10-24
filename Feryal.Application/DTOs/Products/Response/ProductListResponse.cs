using System;
using System.Collections.Generic;
using System.Text;

namespace Feryal.Application.DTOs.Products.Response
{
    public sealed class ProductListResponse
    {
        public int Id { get; init; }

        public int CategoryId { get; init; }

        public int ProductGroupId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public string? PrimaryImageUrl { get; init; }
    }

}
