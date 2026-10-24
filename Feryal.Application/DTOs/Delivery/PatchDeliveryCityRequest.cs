using System.Text.Json;

namespace Feryal.Application.DTOs.Delivery;

public sealed class PatchDeliveryCityRequest
{
    // Gets or sets the delivery city name as a JSON element to support partial updates.
    public JsonElement Name { get; set; }

    // Gets or sets the delivery fee as a JSON element to support partial updates.
    public JsonElement DeliveryFee { get; set; }

    // Gets or sets the active status of the delivery city as a JSON element to support partial updates.
    public JsonElement IsActive { get; set; }
}