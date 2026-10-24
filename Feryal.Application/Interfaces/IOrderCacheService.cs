using Feryal.Application.DTOs.Orders;
using Feryal.Domain.Enums;

// Defines the contract for caching customer and admin order data and invalidating cached order information.
public interface IOrderCacheService
{
    // Retrieves the paginated orders belonging to a specific user from the cache.
    Task<PagedOrdersResponse> GetMyOrdersAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Retrieves paginated orders for the admin, with optional filters for status, search term, delivery city, and pickup point.
    Task<PagedOrdersResponse> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken);

    // Invalidates the cached order data associated with a specific user.
    Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken);

    // Invalidates cached order data used by the admin.
    Task InvalidateAdminAsync(
        CancellationToken cancellationToken);
}