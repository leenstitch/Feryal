using System.Text.Json;

namespace Feryal.Application.DTOs.Delivery;

public sealed class PatchPickupPointRequest
{
    // Gets or sets the pickup point name as a JSON element to support partial updates.
    public JsonElement Name { get; set; }

    // Gets or sets the pickup point address as a JSON element to support partial updates.
    public JsonElement Address { get; set; }

    // Gets or sets the pickup fee as a JSON element to support partial updates.
    public JsonElement PickupFee { get; set; }

    // Gets or sets the active status of the pickup point as a JSON element to support partial updates.
    public JsonElement IsActive { get; set; }
}