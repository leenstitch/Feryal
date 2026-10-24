using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.ProductGroups;

public class CreateProductGroupRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public bool? IsActive { get; set; }
}