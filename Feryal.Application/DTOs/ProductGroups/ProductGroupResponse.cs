using System;
using System.Collections.Generic;
using System.Text;

namespace Feryal.Application.DTOs.ProductGroups
{
    // Represents the response containing product group details.
    public class ProductGroupResponse
    {
        // Gets or sets the unique identifier of the product group.
        public int Id { get; set; }

        // Gets or sets the name of the product group.
        public string Name { get; set; } = null!;

        // Gets or sets the optional description of the product group.
        public string? Description { get; set; }

        // Gets or sets whether the product group is active.
        public bool IsActive { get; set; }

        // Gets or sets the date and time when the product group was created.
        public DateTimeOffset CreatedAt { get; set; }

        // Gets or sets the date and time when the product group was last updated, if applicable.
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}