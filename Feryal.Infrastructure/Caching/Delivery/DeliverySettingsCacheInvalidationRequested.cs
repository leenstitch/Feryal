namespace Feryal.Application.Events.Delivery;

public sealed record DeliverySettingsCacheInvalidationRequested(
    int? DeliveryCityId,
    int? PickupPointId);