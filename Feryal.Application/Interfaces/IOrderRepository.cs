using Feryal.Application.DTOs.Orders;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;

namespace Feryal.Application.Interfaces;

public interface IOrderRepository
{
    Task<Cart?> GetCartForCheckoutAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<CheckoutCustomerProfile?> GetCustomerProfileAsync(
        int userId,
        CancellationToken cancellationToken);

    Task UpdateCustomerProfileAsync(
        int userId,
        string phoneNumber,
        int? deliveryCityId,
        string? area,
        string? street,
        string? buildingNumber,
        string? additionalDetails,
        CancellationToken cancellationToken);

    Task ClearCartAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<List<DeliveryCity>> GetActiveCitiesAsync(
        CancellationToken cancellationToken);

    Task<List<PickupPoint>> GetActivePickupPointsAsync(
        CancellationToken cancellationToken);

    Task<DeliveryCity?> GetActiveCityAsync(
        int cityId,
        CancellationToken cancellationToken);

    Task<PickupPoint?> GetActivePickupPointAsync(
        int pickupPointId,
        CancellationToken cancellationToken);

    Task<ProductVariant?> GetVariantForOrderAsync(
        int productVariantId,
        CancellationToken cancellationToken);

    Task<bool> TryReserveStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    Task<bool> TryReleaseReservedStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    Task<bool> TryCommitReservedStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    Task<bool> HasActiveReservationAsync(
        int userId,
        CancellationToken cancellationToken);

    Task AddReservationAsync(
        InventoryReservation reservation,
        CancellationToken cancellationToken);

    Task<InventoryReservation?> GetReservationAsync(
        int reservationId,
        int userId,
        CancellationToken cancellationToken);

    Task<InventoryReservation?> GetReservationByIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<Order?> GetOrderForReservationAsync(
        int reservationId,
        int userId,
        CancellationToken cancellationToken);

    Task<bool> TryChangeReservationStatusAsync(
        int reservationId,
        InventoryReservationStatus expectedStatus,
        InventoryReservationStatus newStatus,
        DateTimeOffset? mustExpireAfter,
        CancellationToken cancellationToken);

    Task SetReservationOrderAsync(
       int reservationId,
       int orderId,
       CancellationToken cancellationToken);
    Task<List<InventoryReservation>> GetExpiredReservationsAsync(
        int userId,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken);

    Task<List<InventoryReservation>> GetExpiredReservationsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken);

    Task AddOrderAsync(
        Order order,
        CancellationToken cancellationToken);

    Task<Order?> GetOrderByIdAsync(
        int orderId,
        CancellationToken cancellationToken);

    Task<Order?> GetOrderByIdForUserAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken);

    Task<Order?> GetOrderByIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken cancellationToken);
   
    Task<(List<Order> Items, int TotalCount)> GetMyOrdersAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(List<Order> Items, int TotalCount)> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken);
    Task<bool> TryConvertAndLinkReservationAsync(
    int reservationId,
    int orderId,
    DateTimeOffset now,
    CancellationToken cancellationToken);
    //Task<bool> TryRestoreStockAsync(
    //int productVariantId,
    //int quantity,
    //CancellationToken cancellationToken);
}