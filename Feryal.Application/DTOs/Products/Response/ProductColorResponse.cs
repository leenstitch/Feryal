using System;
using System.Collections.Generic;
using System.Text;

namespace Feryal.Application.DTOs.Products.Response
{
    public sealed class ProductColorResponse
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? HexCode { get; init; }

        public bool IsActive { get; init; }
    }
}
