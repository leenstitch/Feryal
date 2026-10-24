namespace Feryal.Application.Services;

// Defines shared cache tags, key prefixes, and expiration settings for order caching.
public static class OrderCacheConstants
{
    // Tag used to group cached order entries for admin-level invalidation.
    public const string AdminTag = "orders:admin";

    // Prefix used to generate cache tags for individual users' orders.
    public const string UserTagPrefix = "orders:user:";

    // Prefix used to generate versioned cache keys for individual users' orders.
    public const string UserKeyPrefix = "orders:v1:user:";

    // Prefix used to generate versioned cache keys for admin order queries.
    public const string AdminKeyPrefix = "orders:v1:admin:";

    // Defines how long order cache entries remain valid in the distributed cache.
    public static readonly TimeSpan Expiration =
        TimeSpan.FromMinutes(5);

    // Defines how long order cache entries remain valid in the local cache.
    public static readonly TimeSpan LocalExpiration =
        TimeSpan.FromMinutes(1);
}