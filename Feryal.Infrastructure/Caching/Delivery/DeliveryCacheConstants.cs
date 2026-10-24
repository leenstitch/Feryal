
namespace Feryal.Infrastructure.Caching.Delivery;

public static class DeliveryCacheConstants
{
    public const string DeliveryCityListsTag = "delivery-cities:lists";

    public const string PickupPointListsTag = "pickup-points:lists";

    public static string GetDeliveryCityTag(int cityId)
        => $"delivery-city:{cityId}";

    public static string GetPickupPointTag(int pickupPointId)
        => $"pickup-point:{pickupPointId}";

    public static string GetDeliveryCityListKey(
        int pageNumber,
        int pageSize,
        bool activeOnly)
        => $"delivery-cities:list:{pageNumber}:{pageSize}:{activeOnly}";

    public static string GetDeliveryCityDetailsKey(int cityId)
        => $"delivery-cities:details:{cityId}";

    public static string GetPickupPointListKey(
        int pageNumber,
        int pageSize,
        bool activeOnly)
        => $"pickup-points:list:{pageNumber}:{pageSize}:{activeOnly}";

    public static string GetPickupPointDetailsKey(int pickupPointId)
        => $"pickup-points:details:{pickupPointId}";
}
