
using System.Text.Json;

namespace Feryal.Application.DTOs.ProductGroups;

public class UpdateProductGroupRequest
{
    public JsonElement Name { get; set; }

    public JsonElement Description { get; set; }
}