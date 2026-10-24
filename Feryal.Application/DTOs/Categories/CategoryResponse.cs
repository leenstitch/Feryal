namespace Feryal.Application.DTOs.Categories;

public class CategoryResponse
{
    // Gets or sets the unique identifier of the category.
    public int Id { get; set; }

    // Gets or sets the name of the category.
    public string Name { get; set; } = string.Empty;

    // Gets or sets the description of the category.
    public string? Description { get; set; }

    // Gets or sets a value indicating whether the category is active.
    public bool IsActive { get; set; }

    // Gets or sets the date and time when the category was created.
    public DateTimeOffset CreatedAt { get; set; }

    // Gets or sets the date and time when the category was last updated.
    public DateTimeOffset? UpdatedAt { get; set; }
}