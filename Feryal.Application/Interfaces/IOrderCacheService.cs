using Feryal.Application.DTOs.Orders;
using Feryal.Domain.Enums;

namespace Feryal.Application.Interfaces;

public interface IOrderCacheService
{
    Task<PagedOrdersResponse> GetMyOrdersAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedOrdersResponse> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken);

    Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken);

    Task InvalidateAdminAsync(
        CancellationToken cancellationToken);
}