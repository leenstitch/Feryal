using System.Text.Json;

namespace Feryal.Application.DTOs.Categories;

public class UpdateCategoryRequest
{
    // Gets or sets the category name as a JSON element to support partial updates.
    public JsonElement? Name { get; set; }

    // Gets or sets the category description as a JSON element to support partial updates.
    public JsonElement? Description { get; set; }

}