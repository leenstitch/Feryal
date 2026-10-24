using System.Text.Json;

namespace Feryal.Application.DTOs.Categories;

public class UpdateCategoryRequest
{
    public JsonElement? Name { get; set; }

    public JsonElement? Description { get; set; }

}