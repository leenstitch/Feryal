using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Categories;

public class CreateCategoryRequest
{
    // Gets or sets the name of the category to be created.
    [Required]
    public string Name { get; set; } = string.Empty;

    // Gets or sets the optional description of the category.
    public string? Description { get; set; }

    // Gets or sets whether the category should be active when created.
    [Required]
    public bool? IsActive { get; set; }
}