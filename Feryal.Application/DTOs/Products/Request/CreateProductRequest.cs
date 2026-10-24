using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Products.Request
{
    // Represents the request used to create a new product with its category, group, and options.
    public sealed class CreateProductRequest
    {
        // Gets or initializes the ID of the category to which the product belongs; must be a positive integer.
        [Range(1, int.MaxValue)]
        public int CategoryId { get; init; }

        // Gets or initializes the ID of the product group to which the product belongs; must be a positive integer.
        [Range(1, int.MaxValue)]
        public int ProductGroupId { get; init; }

        // Gets or initializes the required product name, limited to 200 characters.
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;

        // Gets or initializes the optional product description, limited to 2000 characters.
        [StringLength(2000)]
        public string? Description { get; init; }

        // Gets or initializes the list of colors associated with the product.
        public List<CreateProductColorRequest> Colors { get; init; } = [];

        // Gets or initializes the list of sizes associated with the product.
        public List<CreateProductSizeRequest> Sizes { get; init; } = [];

        // Gets or initializes the list of scents associated with the product.
        public List<CreateProductScentRequest> Scents { get; init; } = [];
    }
}