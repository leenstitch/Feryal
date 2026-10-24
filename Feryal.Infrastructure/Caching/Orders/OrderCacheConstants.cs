namespace Feryal.Application.Services;

public static class OrderCacheConstants
{
    public const string AdminTag = "orders:admin";

    public const string UserTagPrefix = "orders:user:";

    public const string UserKeyPrefix = "orders:v1:user:";

    public const string AdminKeyPrefix = "orders:v1:admin:";

    public static readonly TimeSpan Expiration =
        TimeSpan.FromMinutes(5);

    public static readonly TimeSpan LocalExpiration =
        TimeSpan.FromMinutes(1);
}