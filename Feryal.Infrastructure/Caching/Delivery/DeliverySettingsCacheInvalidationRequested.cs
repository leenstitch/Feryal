namespace Feryal.Application.Events.Delivery;

// Represents a request to invalidate delivery city or pickup point cache entries.
public sealed record DeliverySettingsCacheInvalidationRequested(
    // Identifies the delivery city to invalidate, or null to target all delivery cities.
    int? DeliveryCityId,

    // Identifies the pickup point to invalidate, or null to target all pickup points.
    int? PickupPointId);