using Feryal.Application.DTOs.Orders;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Provides database operations for checkout, customer profiles, inventory reservations, and orders.
public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    // Inject the database context for accessing order-related entities.
    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // CART
    // =========================================================

    // Retrieves the user's cart and related product details for checkout.
    public async Task<Cart?> GetCartForCheckoutAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return await _context.Carts
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            // Load cart items and their associated products and primary images.
            .Include(x => x.Items)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            // Load the selected color, size, and scent for each variant.
            .Include(x => x.Items)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Color)
            .Include(x => x.Items)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Size)
            .Include(x => x.Items)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Scent)
            // Execute the related collection queries separately to avoid join duplication.
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Removes all items from the user's cart and updates its modification timestamp.
    public async Task ClearCartAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        // Find the user's cart ID without loading the entire cart.
        var cartId = await _context.Carts
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Stop if the user does not have a cart.
        if (!cartId.HasValue)
        {
            return;
        }

        // Delete all items belonging to the cart directly in the database.
        await _context.CartItems
            .Where(x => x.CartId == cartId.Value)
            .ExecuteDeleteAsync(cancellationToken);

        // Update the cart's last-modified timestamp.
        await _context.Carts
            .Where(x => x.Id == cartId.Value)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.UpdatedAt,
                    DateTimeOffset.UtcNow),
                cancellationToken);
    }

    // =========================================================
    // CUSTOMER PROFILE
    // =========================================================

    // Retrieves the customer's contact information and delivery address.
    public async Task<CheckoutCustomerProfile?> GetCustomerProfileAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            // Project only the fields required by the checkout customer profile.
            .Select(x => new CheckoutCustomerProfile(
                x.Id,
                x.FirstName,
                x.LastName,
                x.PhoneNumber,
                x.DeliveryCityId,
                x.Area,
                x.Street,
                x.BuildingNumber,
                x.AdditionalDetails))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Updates the customer's phone number and delivery address.
    public async Task UpdateCustomerProfileAsync(
     int userId,
     string phoneNumber,
     int? deliveryCityId,
     string? area,
     string? street,
     string? buildingNumber,
     string? additionalDetails,
     CancellationToken cancellationToken)
    {
        // Ensure the phone number contains a non-whitespace value.
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException(
                "Phone number is required.",
                nameof(phoneNumber));
        }

        // Remove leading and trailing whitespace from the phone number.
        phoneNumber = phoneNumber.Trim();

        // Enforce the maximum supported phone number length.
        if (phoneNumber.Length > 20)
        {
            throw new ArgumentException(
                "Phone number cannot exceed 20 characters.",
                nameof(phoneNumber));
        }

        // Find the customer account to update.
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        // Stop if the customer account does not exist.
        if (user is null)
        {
            throw new KeyNotFoundException(
                "Customer account was not found.");
        }

        // Update the customer's phone number.
        user.PhoneNumber = phoneNumber;

        // Update the customer's delivery address fields.
        user.UpdateAddress(
            deliveryCityId,
            area,
            street,
            buildingNumber,
            additionalDetails);

        // Persist the profile changes.
        await _context.SaveChangesAsync(cancellationToken);
    }

    // =========================================================
    // DELIVERY CITIES
    // =========================================================

    // Retrieves all active delivery cities in ID order.
    public async Task<List<DeliveryCity>> GetActiveCitiesAsync(
        CancellationToken cancellationToken)
    {
        return await _context.DeliveryCities
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    // Retrieves a delivery city by ID only if it is active.
    public async Task<DeliveryCity?> GetActiveCityAsync(
        int cityId,
        CancellationToken cancellationToken)
    {
        return await _context.DeliveryCities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == cityId && x.IsActive,
                cancellationToken);
    }

    // =========================================================
    // PICKUP POINTS
    // =========================================================

    // Retrieves all active pickup points in ID order.
    public async Task<List<PickupPoint>> GetActivePickupPointsAsync(
        CancellationToken cancellationToken)
    {
        return await _context.PickupPoints
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    // Retrieves a pickup point by ID only if it is active.
    public async Task<PickupPoint?> GetActivePickupPointAsync(
        int pickupPointId,
        CancellationToken cancellationToken)
    {
        return await _context.PickupPoints
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == pickupPointId && x.IsActive,
                cancellationToken);
    }

    // =========================================================
    // PRODUCT VARIANTS
    // =========================================================

    // Retrieves a product variant and its related product and option details for an order.
    public async Task<ProductVariant?> GetVariantForOrderAsync(
        int productVariantId,
        CancellationToken cancellationToken)
    {
        return await _context.ProductVariants
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Color)
            .Include(x => x.Size)
            .Include(x => x.Scent)
            .FirstOrDefaultAsync(
                x => x.Id == productVariantId,
                cancellationToken);
    }

    // Attempts to reserve stock for a product variant without reducing its physical stock.
    public async Task<bool> TryReserveStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        // Validate the variant ID and requested quantity.
        ValidatePositive(productVariantId, nameof(productVariantId));
        ValidatePositive(quantity, nameof(quantity));

        // Increase reserved stock only if the variant is eligible and sufficient stock remains.
        var affectedRows = await _context.ProductVariants
            .Where(x =>
                x.Id == productVariantId &&
                x.IsActive &&
                x.IsConfigured &&
                x.Price.HasValue &&
                x.Price.Value >= 0 &&
                x.Product.IsActive &&
                x.StockQuantity.HasValue &&
                x.StockQuantity.Value >= x.ReservedQuantity &&
                x.StockQuantity.Value - x.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.ReservedQuantity,
                    x => x.ReservedQuantity + quantity),
                cancellationToken);

        // Return true only if exactly one variant was updated.
        return affectedRows == 1;
    }

    // Attempts to release a previously reserved quantity of stock.
    public async Task<bool> TryReleaseReservedStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        // Validate the variant ID and quantity.
        ValidatePositive(productVariantId, nameof(productVariantId));
        ValidatePositive(quantity, nameof(quantity));

        // Decrease reserved stock only when enough quantity is currently reserved.
        var affectedRows = await _context.ProductVariants
            .Where(x =>
                x.Id == productVariantId &&
                x.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.ReservedQuantity,
                    x => x.ReservedQuantity - quantity),
                cancellationToken);

        // Return true only if the reservation quantity was successfully released.
        return affectedRows == 1;
    }

    // Commits a stock reservation by reducing both physical and reserved quantities.
    public async Task<bool> TryCommitReservedStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        // Validate the variant ID and quantity.
        ValidatePositive(productVariantId, nameof(productVariantId));
        ValidatePositive(quantity, nameof(quantity));

        // Update stock atomically only when both quantities are sufficient.
        var affectedRows = await _context.ProductVariants
            .Where(x =>
                x.Id == productVariantId &&
                x.StockQuantity.HasValue &&
                x.StockQuantity.Value >= quantity &&
                x.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    // Deduct the purchased quantity from physical stock.
                    .SetProperty(
                        x => x.StockQuantity,
                        x => x.StockQuantity!.Value - quantity)
                    // Release the corresponding reserved quantity.
                    .SetProperty(
                        x => x.ReservedQuantity,
                        x => x.ReservedQuantity - quantity),
                cancellationToken);

        // Return true only if the stock reservation was committed successfully.
        return affectedRows == 1;
    }

    // =========================================================
    // INVENTORY RESERVATIONS
    // =========================================================

    // Checks whether the user has an active reservation that has not expired.
    public async Task<bool> HasActiveReservationAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        // Use UTC time to evaluate reservation expiration.
        var now = DateTimeOffset.UtcNow;

        return await _context.InventoryReservations
            .AnyAsync(
                x => x.UserId == userId &&
                     x.Status == InventoryReservationStatus.Active &&
                     x.ExpiresAt > now,
                cancellationToken);
    }

    // Adds a new inventory reservation to the database context.
    public async Task AddReservationAsync(
        InventoryReservation reservation,
        CancellationToken cancellationToken)
    {
        // Reject a null reservation before attempting to add it.
        ArgumentNullException.ThrowIfNull(reservation);

        await _context.InventoryReservations.AddAsync(
            reservation,
            cancellationToken);
    }

    // Retrieves a reservation belonging to the specified user, including its items.
    public async Task<InventoryReservation?> GetReservationAsync(
        int reservationId,
        int userId,
        CancellationToken cancellationToken)
    {
        return await _context.InventoryReservations
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == reservationId &&
                     x.UserId == userId,
                cancellationToken);
    }

    // Retrieves a reservation using the user's idempotency key.
    public async Task<InventoryReservation?> GetReservationByIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await _context.InventoryReservations
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.UserId == userId &&
                     x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    // Retrieves the order associated with a reservation, provided it belongs to the user.
    public async Task<Order?> GetOrderForReservationAsync(
        int reservationId,
        int userId,
        CancellationToken cancellationToken)
    {
        // Find the order ID linked to the specified user's reservation.
        var orderId = await _context.InventoryReservations
            .Where(x =>
                x.Id == reservationId &&
                x.UserId == userId)
            .Select(x => x.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        // Return null if the reservation does not have a linked order.
        if (!orderId.HasValue)
        {
            return null;
        }

        // Retrieve the associated order and its items without tracking.
        return await _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == orderId.Value &&
                     x.UserId == userId,
                cancellationToken);
    }

    // Changes a reservation's status only when its current status matches the expected status.
    public async Task<bool> TryChangeReservationStatusAsync(
        int reservationId,
        InventoryReservationStatus expectedStatus,
        InventoryReservationStatus newStatus,
        DateTimeOffset? mustExpireAfter,
        CancellationToken cancellationToken)
    {
        // Match the reservation ID and expected current status.
        var query = _context.InventoryReservations
            .Where(x =>
                x.Id == reservationId &&
                x.Status == expectedStatus);

        // Apply the expiration condition when one is provided.
        if (mustExpireAfter.HasValue)
        {
            query = query.Where(
                x => x.ExpiresAt <= mustExpireAfter.Value);
        }

        // Update the status and modification timestamp atomically.
        var affectedRows = await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(x => x.Status, newStatus)
                .SetProperty(
                    x => x.UpdatedAt,
                    DateTimeOffset.UtcNow),
            cancellationToken);

        // Return true only if one reservation was updated.
        return affectedRows == 1;
    }

    // Links an active reservation to an order if it has not already been linked.
    public async Task SetReservationOrderAsync(
        int reservationId,
        int orderId,
        CancellationToken cancellationToken)
    {
        // Validate both IDs.
        ValidatePositive(reservationId, nameof(reservationId));
        ValidatePositive(orderId, nameof(orderId));

        // Link the order only if the reservation is active and currently unlinked.
        var affectedRows = await _context.InventoryReservations
            .Where(x =>
                x.Id == reservationId &&
                x.Status == InventoryReservationStatus.Active &&
                x.OrderId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.OrderId, (int?)orderId)
                    .SetProperty(
                        x => x.UpdatedAt,
                        DateTimeOffset.UtcNow),
                cancellationToken);

        // Fail if the reservation is inactive or already linked to an order.
        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                "The reservation is no longer active or has already been linked to an order.");
        }
    }

    // Retrieves a batch of expired active reservations belonging to a specific user.
    public async Task<List<InventoryReservation>> GetExpiredReservationsAsync(
        int userId,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        // Validate the user ID and requested batch size.
        ValidatePositive(userId, nameof(userId));
        ValidatePositive(batchSize, nameof(batchSize));

        // Return the earliest-expiring reservations first, including their items.
        return await _context.InventoryReservations
            .Where(x =>
                x.UserId == userId &&
                x.Status == InventoryReservationStatus.Active &&
                x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt)
            .Take(batchSize)
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);
    }

    // Retrieves a batch of expired active reservations across all users.
    public async Task<List<InventoryReservation>> GetExpiredReservationsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        // Validate the requested batch size.
        ValidatePositive(batchSize, nameof(batchSize));

        // Return the earliest-expiring reservations first, including their items.
        return await _context.InventoryReservations
            .Where(x =>
                x.Status == InventoryReservationStatus.Active &&
                x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt)
            .Take(batchSize)
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);
    }

    // =========================================================
    // ORDERS
    // =========================================================

    // Adds a new order to the database context.
    public async Task AddOrderAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        // Reject a null order before attempting to add it.
        ArgumentNullException.ThrowIfNull(order);

        await _context.Orders.AddAsync(order, cancellationToken);
    }

    // Retrieves an order by ID, including its items and primary product images.
    public async Task<Order?> GetOrderByIdAsync(
     int orderId,
     CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .FirstOrDefaultAsync(
                x => x.Id == orderId,
                cancellationToken);
    }

    // Retrieves an order only if it belongs to the specified user.
    public async Task<Order?> GetOrderByIdForUserAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .FirstOrDefaultAsync(
                x => x.Id == orderId &&
                     x.UserId == userId,
                cancellationToken);
    }

    // Retrieves an order using the user's idempotency key to prevent duplicate processing.
    public async Task<Order?> GetOrderByIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.UserId == userId &&
                     x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    // Retrieves a paginated list of orders belonging to a specific customer.
    public async Task<(List<Order> Items, int TotalCount)> GetMyOrdersAsync(
     int userId,
     int pageNumber,
     int pageSize,
     CancellationToken cancellationToken)
    {
        // Validate pagination parameters.
        ValidatePositive(pageNumber, nameof(pageNumber));
        ValidatePositive(pageSize, nameof(pageSize));

        // Restrict the query to the current user's orders.
        var query = _context.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        // Count the customer's orders before pagination.
        var totalCount = await query.CountAsync(cancellationToken);

        // Retrieve the requested page, newest orders first, with their items and primary images.
        var items = await query
            .OrderByDescending(x => x.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .ToListAsync(cancellationToken);

        // Return the orders and the total matching count.
        return (items, totalCount);
    }

    // Retrieves a paginated list of orders for administration with optional filters.
    public async Task<(List<Order> Items, int TotalCount)> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken)
    {
        // Validate pagination parameters.
        ValidatePositive(pageNumber, nameof(pageNumber));
        ValidatePositive(pageSize, nameof(pageSize));

        // Start with all orders using a read-only query.
        var query = _context.Orders
            .AsNoTracking()
            .AsQueryable();

        // Filter by order status when specified.
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        // Filter by delivery city when specified.
        if (deliveryCityId.HasValue)
        {
            query = query.Where(
                x => x.DeliveryCityId == deliveryCityId.Value);
        }

        // Filter by pickup point when specified.
        if (pickupPointId.HasValue)
        {
            query = query.Where(
                x => x.PickupPointId == pickupPointId.Value);
        }

        // Search order details and delivery information when a search term is provided.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(x =>
                x.OrderNumber.Contains(term) ||
                x.CustomerName.Contains(term) ||
                x.CustomerPhoneNumber.Contains(term) ||
                (x.DeliveryCityName != null &&
                 x.DeliveryCityName.Contains(term)) ||
                (x.PickupPointName != null &&
                 x.PickupPointName.Contains(term)));
        }

        // Count matching orders before applying pagination.
        var totalCount = await query.CountAsync(cancellationToken);

        // Retrieve the requested page, newest orders first, including item and image details.
        var items = await query
            .OrderByDescending(x => x.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .ToListAsync(cancellationToken);

        // Return the filtered orders and their total count.
        return (items, totalCount);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    // Throws an exception when the supplied integer is not positive.
    private static void ValidatePositive(
        int value,
        string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    // Converts an active, unexpired reservation into a converted reservation and links it to an order.
    public async Task<bool> TryConvertAndLinkReservationAsync(
     int reservationId,
     int orderId,
     DateTimeOffset now,
     CancellationToken cancellationToken)
    {
        // Validate the reservation and order IDs.
        ValidatePositive(reservationId, nameof(reservationId));
        ValidatePositive(orderId, nameof(orderId));

        // Perform the status change and order linking atomically if the reservation is still eligible.
        var affectedRows = await _context.InventoryReservations
            .Where(x =>
                x.Id == reservationId &&
                x.Status == InventoryReservationStatus.Active &&
                x.ExpiresAt > now &&
                x.OrderId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    x => x.Status,
                    InventoryReservationStatus.Converted)
                .SetProperty(
                    x => x.OrderId,
                    (int?)orderId)
                .SetProperty(
                    x => x.UpdatedAt,
                    now),
                cancellationToken);

        // Return true only if the reservation was converted and linked successfully.
        return affectedRows == 1;
    }
}