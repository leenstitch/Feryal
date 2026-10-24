using System;
using System.Collections.Generic;
using System.Text;

namespace Feryal.Application.DTOs.ProductGroups
{
    public class ProductGroupResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
