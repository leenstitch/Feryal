using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.ProductGroups;

// Represents the request used to create a new product group.
public class CreateProductGroupRequest
{
    // Gets or sets the required name of the product group, limited to 100 characters.
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    // Gets or sets the optional description of the product group, limited to 2000 characters.
    [StringLength(2000)]
    public string? Description { get; set; }

    // Gets or sets the required active status of the product group.
    [Required]
    public bool? IsActive { get; set; }
}