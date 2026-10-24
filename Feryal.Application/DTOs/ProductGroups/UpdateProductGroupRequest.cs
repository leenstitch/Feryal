using System.Text.Json;

namespace Feryal.Application.DTOs.ProductGroups;

// Represents a request to update the name or description of a product group.
public class UpdateProductGroupRequest
{
    // Gets or sets the name value provided for the update, allowing detection of its JSON state.
    public JsonElement Name { get; set; }

    // Gets or sets the description value provided for the update, allowing detection of its JSON state.
    public JsonElement Description { get; set; }
}