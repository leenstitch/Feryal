using Feryal.Application.DTOs.Orders;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // CART
    // =========================================================

    public async Task<Cart?> GetCartForCheckoutAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return await _context.Carts
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Items)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            .Include(x => x.Items)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Color)
            .Include(x => x.Items)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Size)
            .Include(x => x.Items)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Scent)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ClearCartAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var cartId = await _context.Carts
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!cartId.HasValue)
        {
            return;
        }

        await _context.CartItems
            .Where(x => x.CartId == cartId.Value)
            .ExecuteDeleteAsync(cancellationToken);

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

    public async Task<CheckoutCustomerProfile?> GetCustomerProfileAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
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
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException(
                "Phone number is required.",
                nameof(phoneNumber));
        }

        phoneNumber = phoneNumber.Trim();

        if (phoneNumber.Length > 20)
        {
            throw new ArgumentException(
                "Phone number cannot exceed 20 characters.",
                nameof(phoneNumber));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "Customer account was not found.");
        }

        user.PhoneNumber = phoneNumber;

        user.UpdateAddress(
            deliveryCityId,
            area,
            street,
            buildingNumber,
            additionalDetails);

        await _context.SaveChangesAsync(cancellationToken);
    }

    // =========================================================
    // DELIVERY CITIES
    // =========================================================

    public async Task<List<DeliveryCity>> GetActiveCitiesAsync(
        CancellationToken cancellationToken)
    {
        return await _context.DeliveryCities
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

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

    public async Task<List<PickupPoint>> GetActivePickupPointsAsync(
        CancellationToken cancellationToken)
    {
        return await _context.PickupPoints
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

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

    public async Task<bool> TryReserveStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        ValidatePositive(productVariantId, nameof(productVariantId));
        ValidatePositive(quantity, nameof(quantity));

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

        return affectedRows == 1;
    }

    public async Task<bool> TryReleaseReservedStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        ValidatePositive(productVariantId, nameof(productVariantId));
        ValidatePositive(quantity, nameof(quantity));

        var affectedRows = await _context.ProductVariants
            .Where(x =>
                x.Id == productVariantId &&
                x.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.ReservedQuantity,
                    x => x.ReservedQuantity - quantity),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryCommitReservedStockAsync(
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        ValidatePositive(productVariantId, nameof(productVariantId));
        ValidatePositive(quantity, nameof(quantity));

        var affectedRows = await _context.ProductVariants
            .Where(x =>
                x.Id == productVariantId &&
                x.StockQuantity.HasValue &&
                x.StockQuantity.Value >= quantity &&
                x.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.StockQuantity,
                        x => x.StockQuantity!.Value - quantity)
                    .SetProperty(
                        x => x.ReservedQuantity,
                        x => x.ReservedQuantity - quantity),
                cancellationToken);

        return affectedRows == 1;
    }

    // =========================================================
    // INVENTORY RESERVATIONS
    // =========================================================

    public async Task<bool> HasActiveReservationAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.InventoryReservations
            .AnyAsync(
                x => x.UserId == userId &&
                     x.Status == InventoryReservationStatus.Active &&
                     x.ExpiresAt > now,
                cancellationToken);
    }

    public async Task AddReservationAsync(
        InventoryReservation reservation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        await _context.InventoryReservations.AddAsync(
            reservation,
            cancellationToken);
    }

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

    public async Task<Order?> GetOrderForReservationAsync(
        int reservationId,
        int userId,
        CancellationToken cancellationToken)
    {
        var orderId = await _context.InventoryReservations
            .Where(x =>
                x.Id == reservationId &&
                x.UserId == userId)
            .Select(x => x.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!orderId.HasValue)
        {
            return null;
        }

        return await _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == orderId.Value &&
                     x.UserId == userId,
                cancellationToken);
    }

    public async Task<bool> TryChangeReservationStatusAsync(
        int reservationId,
        InventoryReservationStatus expectedStatus,
        InventoryReservationStatus newStatus,
        DateTimeOffset? mustExpireAfter,
        CancellationToken cancellationToken)
    {
        var query = _context.InventoryReservations
            .Where(x =>
                x.Id == reservationId &&
                x.Status == expectedStatus);

        if (mustExpireAfter.HasValue)
        {
            query = query.Where(
                x => x.ExpiresAt <= mustExpireAfter.Value);
        }

        var affectedRows = await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(x => x.Status, newStatus)
                .SetProperty(
                    x => x.UpdatedAt,
                    DateTimeOffset.UtcNow),
            cancellationToken);

        return affectedRows == 1;
    }


    public async Task SetReservationOrderAsync(
        int reservationId,
        int orderId,
        CancellationToken cancellationToken)
    {
        ValidatePositive(reservationId, nameof(reservationId));
        ValidatePositive(orderId, nameof(orderId));

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

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                "The reservation is no longer active or has already been linked to an order.");
        }
    }


    public async Task<List<InventoryReservation>> GetExpiredReservationsAsync(
        int userId,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        ValidatePositive(userId, nameof(userId));
        ValidatePositive(batchSize, nameof(batchSize));

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

    public async Task<List<InventoryReservation>> GetExpiredReservationsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        ValidatePositive(batchSize, nameof(batchSize));

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

    public async Task AddOrderAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);

        await _context.Orders.AddAsync(order, cancellationToken);
    }

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

    public async Task<(List<Order> Items, int TotalCount)> GetMyOrdersAsync(
     int userId,
     int pageNumber,
     int pageSize,
     CancellationToken cancellationToken)
    {
        ValidatePositive(pageNumber, nameof(pageNumber));
        ValidatePositive(pageSize, nameof(pageSize));

        var query = _context.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken)
    {
        ValidatePositive(pageNumber, nameof(pageNumber));
        ValidatePositive(pageSize, nameof(pageSize));

        var query = _context.Orders
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (deliveryCityId.HasValue)
        {
            query = query.Where(
                x => x.DeliveryCityId == deliveryCityId.Value);
        }

        if (pickupPointId.HasValue)
        {
            query = query.Where(
                x => x.PickupPointId == pickupPointId.Value);
        }

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

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
    // HELPERS
    // =========================================================

    private static void ValidatePositive(
        int value,
        string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }


    public async Task<bool> TryConvertAndLinkReservationAsync(
     int reservationId,
     int orderId,
     DateTimeOffset now,
     CancellationToken cancellationToken)
    {
        ValidatePositive(reservationId, nameof(reservationId));
        ValidatePositive(orderId, nameof(orderId));

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

        return affectedRows == 1;
    }



}