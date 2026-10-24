using System.Text.Json;

namespace Feryal.Application.DTOs.Delivery;

public sealed class PatchDeliveryCityRequest
{
    public JsonElement Name { get; set; }

    public JsonElement DeliveryFee { get; set; }

    public JsonElement IsActive { get; set; }
}