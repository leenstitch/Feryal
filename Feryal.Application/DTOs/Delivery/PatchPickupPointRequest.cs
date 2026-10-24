using System.Text.Json;

namespace Feryal.Application.DTOs.Delivery;

public sealed class PatchPickupPointRequest
{
    public JsonElement Name { get; set; }

    public JsonElement Address { get; set; }

    public JsonElement PickupFee { get; set; }

    public JsonElement IsActive { get; set; }
}