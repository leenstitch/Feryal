using System.ComponentModel.DataAnnotations;


namespace Feryal.Application.DTOs.Products.Request
{
    public sealed class CreateProductRequest
    {
        [Range(1, int.MaxValue)]
        public int CategoryId { get; init; }

        [Range(1, int.MaxValue)]
        public int ProductGroupId { get; init; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; init; }

        public List<CreateProductColorRequest> Colors { get; init; } = [];

        public List<CreateProductSizeRequest> Sizes { get; init; } = [];

        public List<CreateProductScentRequest> Scents { get; init; } = [];
    }
}
