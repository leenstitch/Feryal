using System.Data;
using System.Text.Json;
using Feryal.Application.DTOs.Orders;
using Feryal.Application.Events.Orders;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;

namespace Feryal.Application.Services;
// Handles checkout, inventory reservations, order confirmation, and order retrieval and management.

public sealed class OrderService : IOrderService
{
    // Defines how long an inventory reservation remains active before expiring.

    private static readonly TimeSpan ReservationLifetime =
        TimeSpan.FromMinutes(10);

    private readonly IOrderRepository _orders;
    private readonly IOrderCacheService _orderCache;
    private readonly ICartCacheService _cartCache;
    private readonly IOutboxMessageRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionManager _transactions;

    public OrderService(
        IOrderRepository orders,
        IOrderCacheService orderCache,
        ICartCacheService cartCache,
        IOutboxMessageRepository outbox,
        IUnitOfWork unitOfWork,
        ITransactionManager transactions)
    {
        _orders = orders;
        _orderCache = orderCache;
        _cartCache = cartCache;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _transactions = transactions;
    }


    // Builds the checkout response with cart items, customer details, and available delivery options.
    public async Task<CheckoutResponse> GetCheckoutAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        ValidateId(userId, nameof(userId));

        var user = await _orders.GetCustomerProfileAsync(
            userId,
            cancellationToken)
            ?? throw new KeyNotFoundException(
                "Customer was not found.");

        var cart = await _orders.GetCartForCheckoutAsync(
            userId,
            cancellationToken);

        var items = (cart?.Items ?? Array.Empty<CartItem>())
            .Select(item =>
            {
                var variant = item.ProductVariant;

                decimal finalPrice = variant.FinalPrice ?? 0m;

                bool canOrder =
                    item.Quantity > 0 &&
                    variant.Product is not null &&
                    variant.Product.IsActive &&
                    variant.IsActive &&
                    variant.IsConfigured &&
                    variant.Price.HasValue &&
                    variant.Price.Value >= 0 &&
                    variant.StockQuantity.HasValue &&
                    variant.StockQuantity.Value >= item.Quantity;

                return new CheckoutItemResponse(
       item.Id,
       variant.Id,
       variant.Product.Name,
       BuildVariantDescription(variant),
       variant.Product.Images
           .FirstOrDefault(x => x.IsPrimary)?.ImageUrl,
       variant.Price ?? 0m,
       variant.DiscountPercentage,
       finalPrice,
       item.Quantity,
       variant.StockQuantity,
       decimal.Round(
           finalPrice * item.Quantity,
           2,
           MidpointRounding.AwayFromZero),
       canOrder
   );
            })
            .ToList();

        var cities = await _orders.GetActiveCitiesAsync(
            cancellationToken);

        var pickupPoints = await _orders.GetActivePickupPointsAsync(
            cancellationToken);

        return new CheckoutResponse(
            items,
            decimal.Round(
                items.Sum(x => x.LineTotal),
                2,
                MidpointRounding.AwayFromZero),
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            new SavedDeliveryAddressResponse(
                user.DeliveryCityId,
                user.Area,
                user.Street,
                user.BuildingNumber,
                user.AdditionalDetails),
            cities.Select(x =>
                new DeliveryCityOptionResponse(
                    x.Id,
                    x.Name,
                    x.DeliveryFee))
                .ToList(),
            pickupPoints.Select(x =>
                new PickupPointOptionResponse(
                    x.Id,
                    x.Name,
                    x.Address,
                    x.PickupFee))
                .ToList());
    }

    // Creates an inventory reservation or returns an existing usable reservation for the same idempotency key.

    public async Task<CreateReservationResponse> CreateReservationAsync(
        int userId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ValidateId(userId, nameof(userId));

        string key = RequiredText(
            idempotencyKey,
            200,
            nameof(idempotencyKey));

        var existingReservation =
            await _orders.GetReservationByIdempotencyKeyAsync(
                userId,
                key,
                cancellationToken);

        if (existingReservation is not null)
        {
            var now = DateTimeOffset.UtcNow;

            if (existingReservation.Status ==
                    InventoryReservationStatus.Active &&
                existingReservation.ExpiresAt > now)
            {
                return new CreateReservationResponse(
                    existingReservation.Id,
                    existingReservation.ReservationToken,
                    existingReservation.ExpiresAt,
                    existingReservation.Items
                        .Select(item => new ReservedItemResponse(
                            item.ProductVariantId,
                            item.Quantity))
                        .ToList());
            }

            if (existingReservation.Status ==
                    InventoryReservationStatus.Active &&
                existingReservation.ExpiresAt <= now)
            {
                await _transactions.ExecuteAsync(
                    async token =>
                    {
                        var reservation =
                            await _orders.GetReservationAsync(
                                existingReservation.Id,
                                userId,
                                token);

                        if (reservation is not null)
                        {
                            await ExpireReservationWithinTransactionAsync(
                                reservation,
                                token);
                        }

                        await _unitOfWork.SaveChangesAsync(token);

                        return true;
                    },
                    IsolationLevel.ReadCommitted,
                    cancellationToken);
            }

            throw new InvalidOperationException(
                "This Idempotency-Key belongs to a reservation " +
                "that is no longer usable. Use a new Idempotency-Key.");
        }

        return await CreateReservationCoreAsync(
            userId,
            key,
            cancellationToken);
    }

    // Retrieves a reservation status for the specified customer.
    public async Task<ReservationStatusResponse?> GetReservationStatusAsync(
        int userId,
        int reservationId,
        CancellationToken cancellationToken)
    {
        ValidateId(userId, nameof(userId));
        ValidateId(reservationId, nameof(reservationId));

        var reservation = await _orders.GetReservationAsync(
            reservationId,
            userId,
            cancellationToken);

        if (reservation is null)
        {
            return null;
        }

        string status = reservation.Status.ToString();

        if (reservation.Status == InventoryReservationStatus.Active &&
            reservation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            status = InventoryReservationStatus.Expired.ToString();
        }

        return new ReservationStatusResponse(
            reservation.Id,
            status,
            reservation.ExpiresAt,
            reservation.OrderId);
    }

    // Releases an active reservation and returns its reserved stock to inventory.

    public async Task<bool> ReleaseReservationAsync(
    int userId,
    int reservationId,
    CancellationToken cancellationToken)
    {
        ValidateId(userId, nameof(userId));
        ValidateId(reservationId, nameof(reservationId));

        return await _transactions.ExecuteAsync(
            async token =>
            {
                var reservation = await _orders.GetReservationAsync(
                    reservationId,
                    userId,
                    token);

                if (reservation is null)
                {
                    return false;
                }

                if (reservation.Status != InventoryReservationStatus.Active)
                {
                    return false;
                }

                if (reservation.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    return false;
                }

                bool changed = await _orders.TryChangeReservationStatusAsync(
                    reservation.Id,
                    InventoryReservationStatus.Active,
                    InventoryReservationStatus.Released,
                    null,
                    token);

                if (!changed)
                {
                    return false;
                }

                foreach (var item in reservation.Items
                             .OrderBy(x => x.ProductVariantId))
                {
                    bool released = await _orders.TryReleaseReservedStockAsync(
                        item.ProductVariantId,
                        item.Quantity,
                        token);

                    if (!released)
                    {
                        throw new InvalidOperationException(
                            $"Could not release reserved stock for variant {item.ProductVariantId}.");
                    }
                }

                await _unitOfWork.SaveChangesAsync(token);

                return true;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Confirms an order using the customer's saved home-delivery details.
    public async Task<Order> ConfirmOrderAsync(
        int userId,
        int reservationId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ValidateId(userId, nameof(userId));
        ValidateId(reservationId, nameof(reservationId));

        string key = RequiredText(
            idempotencyKey,
            200,
            nameof(idempotencyKey));

        var existingOrder = await _orders.GetOrderByIdempotencyKeyAsync(
            userId,
            key,
            cancellationToken);

        if (existingOrder is not null)
        {
            return existingOrder;
        }

        var profile = await _orders.GetCustomerProfileAsync(
            userId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Customer was not found.");

        if (string.IsNullOrWhiteSpace(profile.PhoneNumber))
        {
            throw new InvalidOperationException(
                "Customer phone number is required.");
        }

        if (!profile.DeliveryCityId.HasValue)
        {
            throw new InvalidOperationException(
                "A saved delivery city is required. " +
                "Use the ConfirmOrderRequest overload to select a pickup point " +
                "or provide a different delivery address.");
        }

        var request = new ConfirmOrderRequest
        {
            ReservationId = reservationId,
            PhoneNumber = profile.PhoneNumber,
            DeliveryType = DeliveryType.HomeDelivery,
            DeliveryCityId = profile.DeliveryCityId,
            DeliveryArea = profile.Area,
            DeliveryStreet = profile.Street,
            DeliveryBuildingNumber = profile.BuildingNumber,
            DeliveryAdditionalDetails = profile.AdditionalDetails
        };

        var result = await ConfirmOrderAsync(
            userId,
            request,
            key,
            cancellationToken);

        return await _orders.GetOrderByIdAsync(
            result.Id,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "The created order could not be loaded.");
    }

    // Retrieves the customer's orders in a paginated response.
    public async Task<PagedOrdersResponse> GetMyOrdersAsync(
     int userId,
     int pageNumber,
     int pageSize,
     CancellationToken cancellationToken)
    {
        ValidatePagination(userId, pageNumber, pageSize);

        var result = await _orders.GetMyOrdersAsync(
            userId,
            pageNumber,
            pageSize,
            cancellationToken);

        var items = result.Items
            .Select(MapOrder)
            .ToList();

        int totalPages = (int)Math.Ceiling(
            result.TotalCount / (double)pageSize);

        return new PagedOrdersResponse(
            items,
            pageNumber,
            pageSize,
            result.TotalCount,
            totalPages);
    }

    // Retrieves a specific order if it belongs to the specified customer.

    public async Task<Order?> GetOrderByIdAsync(
    int userId,
    int orderId,
    CancellationToken cancellationToken)
    {
        ValidateId(userId, nameof(userId));
        ValidateId(orderId, nameof(orderId));

        return await _orders.GetOrderByIdForUserAsync(
            orderId,
            userId,
            cancellationToken);
    }

    // Retrieves detailed information about one of the customer's orders.

    public async Task<OrderDetailsResponse?> GetMyOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken)
    {
        ValidateId(userId, nameof(userId));
        ValidateId(orderId, nameof(orderId));

        var order = await _orders.GetOrderByIdForUserAsync(
            orderId,
            userId,
            cancellationToken);

        return order is null ? null : MapOrder(order);
    }

    // Retrieves a filtered and paginated list of orders for administrators.
    public async Task<(List<Order> Items, int TotalCount)> GetAdminOrdersAsync(
      int pageNumber,
      int pageSize,
      OrderStatus? status,
      string? search,
      int? deliveryCityId,
      int? pickupPointId,
      CancellationToken cancellationToken)
    {
        ValidatePagination(1, pageNumber, pageSize);

        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (deliveryCityId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryCityId));
        }

        if (pickupPointId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pickupPointId));
        }

        return await _orders.GetAdminOrdersAsync(
            pageNumber,
            pageSize,
            status,
            search,
            deliveryCityId,
            pickupPointId,
            cancellationToken);
    }

    // Retrieves order details for administrative access.
    public async Task<OrderDetailsResponse?> GetAdminOrderAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        ValidateId(orderId, nameof(orderId));

        var order = await _orders.GetOrderByIdAsync(
            orderId,
            cancellationToken);

        return order is null ? null : MapOrder(order);
    }


    // Updates an order status transactionally and schedules order-cache invalidation.

    public async Task<OrderDetailsResponse?> UpdateOrderStatusAsync(
        int orderId,
        OrderStatus status,
        CancellationToken cancellationToken)
    {
        ValidateId(orderId, nameof(orderId));

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        var result = await _transactions.ExecuteAsync(
            async token =>
            {
                var order = await _orders.GetOrderByIdAsync(
                    orderId,
                    token);

                if (order is null)
                {
                    return null;
                }

                order.ChangeStatus(status);

                await _unitOfWork.SaveChangesAsync(token);

                // مؤقتًا: نعيد قراءة الطلب للتحقق من الحالة المحفوظة.
                var savedOrder = await _orders.GetOrderByIdAsync(
                    orderId,
                    token);

                if (savedOrder is null || savedOrder.Status != status)
                {
                    throw new InvalidOperationException(
                        $"Order status was not persisted. " +
                        $"Expected: {status}, " +
                        $"Actual: {savedOrder?.Status}");
                }

                AddOrderCacheOutbox(order.UserId);

                await _unitOfWork.SaveChangesAsync(token);

                return MapOrder(savedOrder);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);

        if (result is not null)
        {
            await _orderCache.InvalidateUserAsync(
                result.UserId,
                cancellationToken);

            await _orderCache.InvalidateAdminAsync(
                cancellationToken);
        }

        return result;
    }

    //public async Task<OrderDetailsResponse?> UpdateOrderStatusAsync(
    //    int orderId,
    //    OrderStatus status,
    //    CancellationToken cancellationToken)
    //{
    //    ValidateId(orderId, nameof(orderId));

    //    if (!Enum.IsDefined(status))
    //    {
    //        throw new ArgumentOutOfRangeException(nameof(status));
    //    }

    //    var result = await _transactions.ExecuteAsync(
    //        async token =>
    //        {
    //            var order = await _orders.GetOrderByIdAsync(
    //                orderId,
    //                token);

    //            if (order is null)
    //            {
    //                return null;
    //            }

    //            order.ChangeStatus(status);

    //            await _unitOfWork.SaveChangesAsync(token);

    //            AddOrderCacheOutbox(order.UserId);

    //            await _unitOfWork.SaveChangesAsync(token);

    //            return MapOrder(order);
    //        },
    //        IsolationLevel.ReadCommitted,
    //        cancellationToken);

    //    if (result is not null)
    //    {
    //        await _orderCache.InvalidateUserAsync(
    //            result.UserId,
    //            cancellationToken);

    //        await _orderCache.InvalidateAdminAsync(
    //            cancellationToken);
    //    }

    //    return result;
    //}

    // Marks a reservation as expired and releases its reserved stock within the current transaction.
    private async Task ExpireReservationWithinTransactionAsync(
        InventoryReservation reservation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        var now = DateTimeOffset.UtcNow;

        bool changed =
            await _orders.TryChangeReservationStatusAsync(
                reservation.Id,
                InventoryReservationStatus.Active,
                InventoryReservationStatus.Expired,
                now,
                cancellationToken);

        if (!changed)
        {
            return;
        }

        foreach (var item in reservation.Items
                     .OrderBy(x => x.ProductVariantId))
        {
            bool released =
                await _orders.TryReleaseReservedStockAsync(
                    item.ProductVariantId,
                    item.Quantity,
                    cancellationToken);

            if (!released)
            {
                throw new InvalidOperationException(
                    $"Could not release reserved stock for variant " +
                    $"{item.ProductVariantId}.");
            }
        }
    }

    // Adds an outbox message to invalidate cached order data for a customer.
    private void AddOrderCacheOutbox(int userId)
    {
        string payload = JsonSerializer.Serialize(
            new OrderCacheInvalidationRequested(userId));

        _outbox.Add(
            new OutboxMessage(
                typeof(OrderCacheInvalidationRequested).FullName!,
                payload));
    }


    // Adds an outbox message to invalidate cached cart data for a customer.
    private void AddCartCacheOutbox(int userId)
    {
        var cacheEvent =
            new Feryal.Application.Events.Cart.CartCacheInvalidationRequested(
                userId);

        string payload = JsonSerializer.Serialize(cacheEvent);

        _outbox.Add(
            new OutboxMessage(
                typeof(Feryal.Application.Events.Cart.CartCacheInvalidationRequested).FullName!,
                payload));
    }

    // Generates a unique order number with a timestamp and GUID.
    private static string GenerateOrderNumber()
    {
        return $"FY-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32];
    }

    // Validates required text, trims whitespace, and enforces a maximum length.
    private static string RequiredText(
        string? value,
        int maxLength,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{field} is required.");
        }

        string result = value.Trim();

        if (result.Length > maxLength)
        {
            throw new ArgumentException(
                $"{field} cannot exceed {maxLength} characters.");
        }

        return result;
    }

    // Normalizes optional text and enforces its maximum length when provided.
    private static string? OptionalText(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string result = value.Trim();

        if (result.Length > maxLength)
        {
            throw new ArgumentException(
                $"Text cannot exceed {maxLength} characters.");
        }

        return result;
    }


    // Builds a readable description from a variant's description, color, size, and scent.
    private static string? BuildVariantDescription(
        ProductVariant variant)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(variant.Description))
        {
            parts.Add(variant.Description.Trim());
        }

        if (variant.Color is not null)
        {
            parts.Add($"Color: {variant.Color.Name}");
        }

        if (variant.Size is not null)
        {
            parts.Add($"Size: {variant.Size.Name}");
        }

        if (variant.Scent is not null)
        {
            parts.Add($"Scent: {variant.Scent.Name}");
        }

        return parts.Count == 0
            ? null
            : string.Join(" | ", parts);
    }


    // Maps an order entity and its items to the order details response DTO.
    private static OrderDetailsResponse MapOrder(Order order)
    {
        var items = order.Items.Select(item =>
            new OrderItemResponse(
                item.Id,
                item.ProductVariantId,
                item.ProductName,
                item.VariantDescription,
                item.ProductVariant?.Product?.Images
                    .FirstOrDefault(x => x.IsPrimary)?.ImageUrl,
                item.Quantity,
                item.UnitPrice,
                item.TotalPrice
                ))
            .ToList();

        return new OrderDetailsResponse(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.CustomerName,
            order.CustomerPhoneNumber,
            order.DeliveryType,
            order.DeliveryCityId,
            order.DeliveryCityName,
            order.DeliveryArea,
            order.DeliveryStreet,
            order.DeliveryBuildingNumber,
            order.DeliveryAdditionalDetails,
            order.PickupPointId,
            order.PickupPointName,
            order.PickupPointAddress,
            order.OrderDate,
            order.Subtotal,
            order.DiscountAmount,
            order.DeliveryFee,
            order.TotalAmount,
            order.Status,
            order.CreatedAt,
            order.UpdatedAt,
            items);
    }

    // Ensures an identifier is a positive integer.
    private static void ValidateId(int id, string name)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                name,
                "ID must be greater than zero.");
        }
    }

    // Validates the customer ID and pagination parameters.
    private static void ValidatePagination(
        int userId,
        int pageNumber,
        int pageSize)
    {
        ValidateId(userId, nameof(userId));

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be between 1 and 100.");
        }
    }

    // Creates a reservation transactionally, validates cart items, and reserves the required stock.
    private async Task<CreateReservationResponse> CreateReservationCoreAsync(
    int userId,
    string idempotencyKey,
    CancellationToken cancellationToken)
    {
        return await _transactions.ExecuteAsync(
            async token =>
            {
                var now = DateTimeOffset.UtcNow;

                var cart = await _orders.GetCartForCheckoutAsync(
                    userId,
                    token);

                if (cart is null || cart.Items.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Your cart is empty.");
                }

                var expiredReservations =
                    await _orders.GetExpiredReservationsAsync(
                        userId,
                        now,
                        100,
                        token);

                foreach (var expired in expiredReservations)
                {
                    await ExpireReservationWithinTransactionAsync(
                        expired,
                        token);
                }

                if (await _orders.HasActiveReservationAsync(userId, token))
                {
                    throw new InvalidOperationException(
                        "You already have an active checkout reservation.");
                }

                var items = cart.Items
                    .OrderBy(x => x.ProductVariantId)
                    .ToList();

                foreach (var item in items)
                {
                    var variant = item.ProductVariant;

                    if (item.Quantity <= 0 ||
                        variant is null ||
                        variant.Product is null ||
                        !variant.Product.IsActive ||
                        !variant.IsActive ||
                        !variant.IsConfigured ||
                        !variant.Price.HasValue ||
                        variant.Price.Value < 0)
                    {
                        throw new InvalidOperationException(
                            $"Variant {item.ProductVariantId} is not available.");
                    }
                }

                foreach (var item in items)
                {
                    bool reserved = await _orders.TryReserveStockAsync(
                        item.ProductVariantId,
                        item.Quantity,
                        token);

                    if (!reserved)
                    {
                        throw new InvalidOperationException(
                            $"There is not enough stock for variant {item.ProductVariantId}.");
                    }
                }

                var reservation = new InventoryReservation(
                    userId,
                    now.Add(ReservationLifetime),
                    Guid.NewGuid().ToString("N"),
                    idempotencyKey);

                await _orders.AddReservationAsync(reservation, token);

                // Save first to obtain the database-generated reservation ID.
                await _unitOfWork.SaveChangesAsync(token);

                foreach (var item in items)
                {
                    reservation.AddItem(
                        new InventoryReservationItem(
                            reservation.Id,
                            item.ProductVariantId,
                            item.Quantity));
                }

                await _unitOfWork.SaveChangesAsync(token);

                return new CreateReservationResponse(
                    reservation.Id,
                    reservation.ReservationToken,
                    reservation.ExpiresAt,
                    items.Select(item => new ReservedItemResponse(
                        item.ProductVariantId,
                        item.Quantity))
                    .ToList());
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }


    // Validates the reservation and delivery details, creates the order, commits stock, and clears the cart.
    public async Task<OrderDetailsResponse> ConfirmOrderAsync(
        int userId,
        ConfirmOrderRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ValidateId(userId, nameof(userId));
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.ReservationId, nameof(request.ReservationId));

        string key = RequiredText(
            idempotencyKey,
            200,
            nameof(idempotencyKey));

        var existingOrder = await _orders.GetOrderByIdempotencyKeyAsync(
            userId,
            key,
            cancellationToken);

        if (existingOrder is not null)
        {
            return MapOrder(existingOrder);
        }

        var result = await _transactions.ExecuteAsync(
            async token =>
            {
                var existingForReservation =
                    await _orders.GetOrderForReservationAsync(
                        request.ReservationId,
                        userId,
                        token);

                if (existingForReservation is not null)
                {
                    return MapOrder(existingForReservation);
                }

                var reservation = await _orders.GetReservationAsync(
                    request.ReservationId,
                    userId,
                    token)
                    ?? throw new KeyNotFoundException(
                        "Reservation was not found.");

                if (reservation.Status != InventoryReservationStatus.Active ||
                    reservation.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    throw new InvalidOperationException(
                        "The reservation has expired or is no longer active.");
                }

                var cart = await _orders.GetCartForCheckoutAsync(
                    userId,
                    token);

                if (cart is null || cart.Items.Count == 0)
                {
                    throw new InvalidOperationException("Your cart is empty.");
                }

                var cartQuantities = cart.Items
                    .Select(x => (x.ProductVariantId, x.Quantity))
                    .OrderBy(x => x.ProductVariantId)
                    .ToList();

                var reservationQuantities = reservation.Items
                    .Select(x => (x.ProductVariantId, x.Quantity))
                    .OrderBy(x => x.ProductVariantId)
                    .ToList();

                if (!cartQuantities.SequenceEqual(reservationQuantities))
                {
                    throw new InvalidOperationException(
                        "Your cart changed after the reservation was created.");
                }

                string phone = RequiredText(
                    request.PhoneNumber,
                    20,
                    nameof(request.PhoneNumber));

                var user = await _orders.GetCustomerProfileAsync(
                    userId,
                    token)
                    ?? throw new KeyNotFoundException("Customer was not found.");

                DeliveryCity? city = null;
                PickupPoint? pickup = null;

                string? cityName = null;
                string? area = null;
                string? street = null;
                string? building = null;
                string? additional = null;
                string? pickupName = null;
                string? pickupAddress = null;

                decimal deliveryFee;

                if (request.DeliveryType == DeliveryType.HomeDelivery)
                {
                    if (!request.DeliveryCityId.HasValue)
                    {
                        throw new ArgumentException("Delivery city is required.");
                    }

                    city = await _orders.GetActiveCityAsync(
                        request.DeliveryCityId.Value,
                        token)
                        ?? throw new InvalidOperationException(
                            "Selected delivery city is unavailable.");

                    cityName = city.Name;

                    area = RequiredText(
                        request.DeliveryArea,
                        150,
                        nameof(request.DeliveryArea));

                    street = RequiredText(
                        request.DeliveryStreet,
                        200,
                        nameof(request.DeliveryStreet));

                    building = RequiredText(
                        request.DeliveryBuildingNumber,
                        50,
                        nameof(request.DeliveryBuildingNumber));

                    additional = OptionalText(
                        request.DeliveryAdditionalDetails,
                        500);

                    deliveryFee = city.DeliveryFee;
                }
                else if (request.DeliveryType == DeliveryType.Pickup)
                {
                    if (!request.PickupPointId.HasValue)
                    {
                        throw new ArgumentException("Pickup point is required.");
                    }

                    pickup = await _orders.GetActivePickupPointAsync(
                        request.PickupPointId.Value,
                        token)
                        ?? throw new InvalidOperationException(
                            "Selected pickup point is unavailable.");

                    pickupName = pickup.Name;
                    pickupAddress = pickup.Address;
                    deliveryFee = pickup.PickupFee;
                }
                else
                {
                    throw new ArgumentException("Unsupported delivery type.");
                }

                var orderLines =
                    new List<(CartItem CartItem, ProductVariant Variant,
                        decimal Price, string? Description)>();

                decimal subtotal = 0m;

                foreach (var cartItem in cart.Items.OrderBy(
                             x => x.ProductVariantId))
                {
                    var variant = await _orders.GetVariantForOrderAsync(
                        cartItem.ProductVariantId,
                        token);

                    if (variant is null ||
                        variant.Product is null ||
                        !variant.Product.IsActive ||
                        !variant.IsActive ||
                        !variant.IsConfigured ||
                        !variant.Price.HasValue ||
                        cartItem.Quantity <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Variant {cartItem.ProductVariantId} is unavailable.");
                    }

                    decimal price = decimal.Round(
                        variant.FinalPrice
                            ?? throw new InvalidOperationException(
                                "Selling price is missing."),
                        2,
                        MidpointRounding.AwayFromZero);

                    if (price < 0)
                    {
                        throw new InvalidOperationException(
                            "Selling price cannot be negative.");
                    }

                    subtotal += price * cartItem.Quantity;

                    orderLines.Add((
                        cartItem,
                        variant,
                        price,
                        BuildVariantDescription(variant)));
                }

                subtotal = decimal.Round(
                    subtotal,
                    2,
                    MidpointRounding.AwayFromZero);

                var order = new Order(
                    user.Id,
                    $"{user.FirstName} {user.LastName}".Trim(),
                    phone,
                    null,
                    request.DeliveryType,
                    city?.Id,
                    pickup?.Id,
                    cityName,
                    area,
                    street,
                    building,
                    additional,
                    pickupName,
                    pickupAddress,
                    GenerateOrderNumber(),
                    key,
                    subtotal,
                    deliveryFee);

                await _orders.AddOrderAsync(order, token);
                await _unitOfWork.SaveChangesAsync(token);

                foreach (var line in orderLines)
                {
                    order.AddItem(
                        new OrderItem(
                            order.Id,
                            line.Variant.Id,
                            line.CartItem.Quantity,
                            line.Price,
                            line.Variant.Product.Name,
                            line.Description));
                }

                // Link the reservation before changing its status.
                var now = DateTimeOffset.UtcNow;

                var converted = await _orders.TryConvertAndLinkReservationAsync(
                    reservation.Id,
                    order.Id,
                    now,
                    token);

               
                if (!converted)
                {
                    throw new InvalidOperationException(
                        "Reservation was already used or expired.");
                }

                foreach (var item in reservation.Items.OrderBy(
                             x => x.ProductVariantId))
                {
                    bool committed = await _orders.TryCommitReservedStockAsync(
                        item.ProductVariantId,
                        item.Quantity,
                        token);

                    if (!committed)
                    {
                        throw new InvalidOperationException(
                            $"Could not commit stock for variant {item.ProductVariantId}.");
                    }
                }

                await _orders.UpdateCustomerProfileAsync(
                    userId,
                    phone,
                    request.DeliveryType == DeliveryType.HomeDelivery
                        ? city!.Id
                        : user.DeliveryCityId,
                    request.DeliveryType == DeliveryType.HomeDelivery
                        ? area
                        : user.Area,
                    request.DeliveryType == DeliveryType.HomeDelivery
                        ? street
                        : user.Street,
                    request.DeliveryType == DeliveryType.HomeDelivery
                        ? building
                        : user.BuildingNumber,
                    request.DeliveryType == DeliveryType.HomeDelivery
                        ? additional
                        : user.AdditionalDetails,
                    token);

                await _orders.ClearCartAsync(userId, token);

                AddOrderCacheOutbox(userId);
                AddCartCacheOutbox(userId);

                await _unitOfWork.SaveChangesAsync(token);

                var savedOrder = await _orders.GetOrderByIdAsync(
                    order.Id,
                    token)
                    ?? throw new InvalidOperationException(
                        "Could not reload the created order.");

                return MapOrder(savedOrder);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);

        await _orderCache.InvalidateUserAsync(userId, cancellationToken);
        await _orderCache.InvalidateAdminAsync(cancellationToken);
        await _cartCache.InvalidateUserAsync(userId, cancellationToken);

        return result;
    }

}