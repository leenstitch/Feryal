
using System.Text.Json;

namespace Feryal.Application.DTOs.Products.Request;

public sealed class PatchProductRequest
{
    public JsonElement CategoryId { get; init; }

    public JsonElement ProductGroupId { get; init; }

    public JsonElement Name { get; init; }

    public JsonElement Description { get; init; }
}
