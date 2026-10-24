using System.Text.Json;

namespace Feryal.Application.DTOs.Products.Request;

// Represents a request to partially update a product's details.
public sealed class PatchProductRequest
{
    // Gets or initializes the category ID value provided for the update, allowing detection of its JSON state.
    public JsonElement CategoryId { get; init; }

    // Gets or initializes the product group ID value provided for the update, allowing detection of its JSON state.
    public JsonElement ProductGroupId { get; init; }

    // Gets or initializes the product name value provided for the update, allowing detection of its JSON state.
    public JsonElement Name { get; init; }

    // Gets or initializes the product description value provided for the update, allowing detection of its JSON state.
    public JsonElement Description { get; init; }
}