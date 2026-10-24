using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Categories;

public class CreateCategoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required]
    public bool? IsActive { get; set; }
}