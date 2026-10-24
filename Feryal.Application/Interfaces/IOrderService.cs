using Feryal.Application.DTOs.Orders;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;

// Defines the contract for checkout, inventory reservations, and order management operations.
public interface IOrderService
{
    // Retrieves the checkout information for the specified user, including cart items and delivery options.
    Task<CheckoutResponse> GetCheckoutAsync(
        int userId,
        CancellationToken cancellationToken);

    // Creates an inventory reservation for the user's cart using an idempotency key to prevent duplicate operations.
    Task<CreateReservationResponse> CreateReservationAsync(
        int userId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    // Retrieves the status of a reservation belonging to the specified user.
    Task<ReservationStatusResponse?> GetReservationStatusAsync(
        int userId,
        int reservationId,
        CancellationToken cancellationToken);

    // Attempts to release an inventory reservation belonging to the specified user.
    Task<bool> ReleaseReservationAsync(
        int userId,
        int reservationId,
        CancellationToken cancellationToken);

    // Confirms an order using the reservation ID and an idempotency key.
    Task<Order> ConfirmOrderAsync(
     int userId,
     int reservationId,
     string idempotencyKey,
     CancellationToken cancellationToken);

    // Confirms an order using the provided order details and returns the complete order response.
    Task<OrderDetailsResponse> ConfirmOrderAsync(
        int userId,
        ConfirmOrderRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    // Retrieves an order by its ID, ensuring it belongs to the specified user.
    Task<Order?> GetOrderByIdAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of orders belonging to the specified user.
    Task<PagedOrdersResponse> GetMyOrdersAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Retrieves the details of a specific order belonging to the specified user.
    Task<OrderDetailsResponse?> GetMyOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken);

    // Retrieves admin orders using optional filters and returns the matching orders with their total count.
    Task<(List<Order> Items, int TotalCount)> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken);

    // Retrieves the details of a specific order for administrative access.
    Task<OrderDetailsResponse?> GetAdminOrderAsync(
        int orderId,
        CancellationToken cancellationToken);

    // Updates an order's status and returns the updated order details.
    Task<OrderDetailsResponse?> UpdateOrderStatusAsync(
        int orderId,
        OrderStatus status,
        CancellationToken cancellationToken);
}