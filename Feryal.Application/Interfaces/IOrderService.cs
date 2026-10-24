using Feryal.Application.DTOs.Orders;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;

namespace Feryal.Application.Interfaces;

public interface IOrderService
{
    Task<CheckoutResponse> GetCheckoutAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<CreateReservationResponse> CreateReservationAsync(
        int userId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ReservationStatusResponse?> GetReservationStatusAsync(
        int userId,
        int reservationId,
        CancellationToken cancellationToken);

    Task<bool> ReleaseReservationAsync(
        int userId,
        int reservationId,
        CancellationToken cancellationToken);

    Task<Order> ConfirmOrderAsync(
     int userId,
     int reservationId,
     string idempotencyKey,
     CancellationToken cancellationToken);

    Task<OrderDetailsResponse> ConfirmOrderAsync(
        int userId,
        ConfirmOrderRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<Order?> GetOrderByIdAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken);

    Task<PagedOrdersResponse> GetMyOrdersAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<OrderDetailsResponse?> GetMyOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken);

    Task<(List<Order> Items, int TotalCount)> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken);

    Task<OrderDetailsResponse?> GetAdminOrderAsync(
        int orderId,
        CancellationToken cancellationToken);

    Task<OrderDetailsResponse?> UpdateOrderStatusAsync(
        int orderId,
        OrderStatus status,
        CancellationToken cancellationToken);
}