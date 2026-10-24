using Feryal.Application.DTOs.Orders;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;

// Defines the contract for database operations related to checkout, inventory reservations, and order management.
public interface IOrderRepository
{
    // Retrieves the user's cart with the data required to complete checkout.
    Task<Cart?> GetCartForCheckoutAsync(
        int userId,
        CancellationToken cancellationToken);

    // Retrieves the customer's profile information used during checkout.
    Task<CheckoutCustomerProfile?> GetCustomerProfileAsync(
        int userId,
        CancellationToken cancellationToken);

    // Updates the customer's phone number and saved delivery address information.
    Task UpdateCustomerProfileAsync(
        int userId,
        string phoneNumber,
        int? deliveryCityId,
        string? area,
        string? street,
        string? buildingNumber,
        string? additionalDetails,
        CancellationToken cancellationToken);

    // Clears all items from the user's cart.
    Task ClearCartAsync(
        int userId,
        CancellationToken cancellationToken);

    // Retrieves all active delivery cities.
    Task<List<DeliveryCity>> GetActiveCitiesAsync(
        CancellationToken cancellationToken);

    // Retrieves all active pickup points.
    Task<List<PickupPoint>> GetActivePickupPointsAsync(
        CancellationToken cancellationToken);

    // Retrieves an active delivery city by its ID.
    Task<DeliveryCity?> GetActiveCityAsync(
        int cityId,
        CancellationToken cancellationToken);

    // Retrieves an active pickup point by its ID.
    Task<PickupPoint?> GetActivePickupPointAsync(
        int pickupPointId,
        CancellationToken cancellationToken);

    // Retrieves a product variant with the information required for order processing.
    Task<ProductVariant?> GetVariantForOrderAsync(
        int productVariantId,
        CancellationToken cancellationToken);

    // Attempts to reserve the specified quantity of a product variant's stock.
    Task<bool> TryReserveStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    // Attempts to release previously reserved stock for a product variant.
    Task<bool> TryReleaseReservedStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    // Attempts to commit reserved stock as finalized inventory deductions.
    Task<bool> TryCommitReservedStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    // Checks whether the user already has an active inventory reservation.
    Task<bool> HasActiveReservationAsync(
        int userId,
        CancellationToken cancellationToken);

    // Adds a new inventory reservation.
    Task AddReservationAsync(
        InventoryReservation reservation,
        CancellationToken cancellationToken);

    // Retrieves an inventory reservation by its ID and the associated user's ID.
    Task<InventoryReservation?> GetReservationAsync(
        int reservationId,
        int userId,
        CancellationToken cancellationToken);

    // Retrieves a user's inventory reservation using its idempotency key.
    Task<InventoryReservation?> GetReservationByIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    // Retrieves the order associated with a reservation for the specified user.
    Task<Order?> GetOrderForReservationAsync(
        int reservationId,
        int userId,
        CancellationToken cancellationToken);

    // Attempts to change a reservation's status if its current status and expiration conditions match.
    Task<bool> TryChangeReservationStatusAsync(
        int reservationId,
        InventoryReservationStatus expectedStatus,
        InventoryReservationStatus newStatus,
        DateTimeOffset? mustExpireAfter,
        CancellationToken cancellationToken);

    // Associates an inventory reservation with an order.
    Task SetReservationOrderAsync(
       int reservationId,
       int orderId,
       CancellationToken cancellationToken);

    // Retrieves a batch of expired reservations belonging to the specified user.
    Task<List<InventoryReservation>> GetExpiredReservationsAsync(
        int userId,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken);

    // Retrieves a batch of expired reservations across all users.
    Task<List<InventoryReservation>> GetExpiredReservationsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken);

    // Adds a new order to the database.
    Task AddOrderAsync(
        Order order,
        CancellationToken cancellationToken);

    // Retrieves an order by its ID.
    Task<Order?> GetOrderByIdAsync(
        int orderId,
        CancellationToken cancellationToken);

    // Retrieves an order by its ID, ensuring it belongs to the specified user.
    Task<Order?> GetOrderByIdForUserAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken);

    // Retrieves an order belonging to a user using its idempotency key.
    Task<Order?> GetOrderByIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of orders belonging to the specified user, along with the total count.
    Task<(List<Order> Items, int TotalCount)> GetMyOrdersAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Retrieves a paginated list of admin orders using optional filters, along with the total count.
    Task<(List<Order> Items, int TotalCount)> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken);

    // Attempts to convert a reservation into an order and link the reservation to that order.
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