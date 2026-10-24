namespace Feryal.Infrastructure.Caching.Delivery;

// Defines shared cache tags and cache key generators for delivery cities and pickup points.
public static class DeliveryCacheConstants
{
    // Tag used to group delivery city list entries for targeted invalidation.
    public const string DeliveryCityListsTag = "delivery-cities:lists";

    // Tag used to group pickup point list entries for targeted invalidation.
    public const string PickupPointListsTag = "pickup-points:lists";

    // Generates a cache tag associated with a specific delivery city.
    public static string GetDeliveryCityTag(int cityId)
        => $"delivery-city:{cityId}";

    // Generates a cache tag associated with a specific pickup point.
    public static string GetPickupPointTag(int pickupPointId)
        => $"pickup-point:{pickupPointId}";

    // Builds a unique cache key for a paginated delivery city list and its active filter.
    public static string GetDeliveryCityListKey(
        int pageNumber,
        int pageSize,
        bool activeOnly)
        => $"delivery-cities:list:{pageNumber}:{pageSize}:{activeOnly}";

    // Builds a cache key for the details of a specific delivery city.
    public static string GetDeliveryCityDetailsKey(int cityId)
        => $"delivery-cities:details:{cityId}";

    // Builds a unique cache key for a paginated pickup point list and its active filter.
    public static string GetPickupPointListKey(
        int pageNumber,
        int pageSize,
        bool activeOnly)
        => $"pickup-points:list:{pageNumber}:{pageSize}:{activeOnly}";

    // Builds a cache key for the details of a specific pickup point.
    public static string GetPickupPointDetailsKey(int pickupPointId)
        => $"pickup-points:details:{pickupPointId}";
}