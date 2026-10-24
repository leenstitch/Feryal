using Feryal.Domain.Enums;

namespace Feryal.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = new();

    private Order()
    {
    }

    public Order(
        int userId,
        string customerName,
        string customerPhoneNumber,
        int? couponId,
        DeliveryType deliveryType,
        int? deliveryCityId,
        int? pickupPointId,
        string? deliveryCityName,
        string? deliveryArea,
        string? deliveryStreet,
        string? deliveryBuildingNumber,
        string? deliveryAdditionalDetails,
        string? pickupPointName,
        string? pickupPointAddress,
        string orderNumber,
        string idempotencyKey,
        decimal subtotal,
        decimal deliveryFee)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException(
                "Customer name is required.",
                nameof(customerName));
        }

        if (string.IsNullOrWhiteSpace(customerPhoneNumber))
        {
            throw new ArgumentException(
                "Customer phone number is required.",
                nameof(customerPhoneNumber));
        }

        if (customerPhoneNumber.Trim().Length > 20)
        {
            throw new ArgumentException(
                "Customer phone number cannot exceed 20 characters.",
                nameof(customerPhoneNumber));
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new ArgumentException(
                "Order number is required.",
                nameof(orderNumber));
        }

        if (orderNumber.Trim().Length > 50)
        {
            throw new ArgumentException(
                "Order number cannot exceed 50 characters.",
                nameof(orderNumber));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));
        }

        if (idempotencyKey.Trim().Length > 200)
        {
            throw new ArgumentException(
                "Idempotency key cannot exceed 200 characters.",
                nameof(idempotencyKey));
        }

        if (subtotal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subtotal));
        }

        if (deliveryFee < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryFee));
        }

        if (!Enum.IsDefined(deliveryType))
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryType));
        }

        if (deliveryType == DeliveryType.HomeDelivery &&
            !deliveryCityId.HasValue)
        {
            throw new ArgumentException(
                "A delivery city is required for home delivery.",
                nameof(deliveryCityId));
        }

        if (deliveryType == DeliveryType.Pickup &&
            !pickupPointId.HasValue)
        {
            throw new ArgumentException(
                "A pickup point is required for pickup orders.",
                nameof(pickupPointId));
        }

        UserId = userId;
        CustomerName = customerName.Trim();
        CustomerPhoneNumber = customerPhoneNumber.Trim();
        CouponId = couponId;

        DeliveryType = deliveryType;
        DeliveryCityId = deliveryCityId;
        PickupPointId = pickupPointId;

        DeliveryCityName = NormalizeOptional(deliveryCityName);
        DeliveryArea = NormalizeOptional(deliveryArea);
        DeliveryStreet = NormalizeOptional(deliveryStreet);
        DeliveryBuildingNumber = NormalizeOptional(deliveryBuildingNumber);
        DeliveryAdditionalDetails = NormalizeOptional(deliveryAdditionalDetails);

        PickupPointName = NormalizeOptional(pickupPointName);
        PickupPointAddress = NormalizeOptional(pickupPointAddress);

        OrderNumber = orderNumber.Trim();
        IdempotencyKey = idempotencyKey.Trim();

        OrderDate = DateTimeOffset.UtcNow;
        CreatedAt = DateTimeOffset.UtcNow;

        Subtotal = RoundMoney(subtotal);
        DiscountAmount = 0m;
        DeliveryFee = RoundMoney(deliveryFee);
        TotalAmount = RoundMoney(Subtotal + DeliveryFee);

        Status = OrderStatus.Processing;
    }

    public int Id { get; private set; }

    public int UserId { get; private set; }

    public string CustomerName { get; private set; } = null!;

    public string CustomerPhoneNumber { get; private set; } = null!;

    public int? CouponId { get; private set; }

    public DeliveryType DeliveryType { get; private set; }

    public int? DeliveryCityId { get; private set; }

    public int? PickupPointId { get; private set; }

    // Snapshot values: they preserve the delivery details used for this order.
    public string? DeliveryCityName { get; private set; }

    public string? DeliveryArea { get; private set; }

    public string? DeliveryStreet { get; private set; }

    public string? DeliveryBuildingNumber { get; private set; }

    public string? DeliveryAdditionalDetails { get; private set; }

    public string? PickupPointName { get; private set; }

    public string? PickupPointAddress { get; private set; }

    public string OrderNumber { get; private set; } = null!;

    public string IdempotencyKey { get; private set; } = null!;

    public DateTimeOffset OrderDate { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal DeliveryFee { get; private set; }

    public decimal TotalAmount { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public void AddItem(OrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (Id > 0 && item.OrderId != Id)
        {
            throw new InvalidOperationException(
                "The order item belongs to a different order.");
        }

        if (_items.Any(x => x.ProductVariantId == item.ProductVariantId))
        {
            throw new InvalidOperationException(
                "The product variant already exists in this order.");
        }

        _items.Add(item);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ApplyDiscount(decimal discountAmount)
    {
        if (discountAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountAmount));
        }

        if (discountAmount > Subtotal + DeliveryFee)
        {
            throw new ArgumentException(
                "Discount cannot exceed the order total.",
                nameof(discountAmount));
        }

        DiscountAmount = RoundMoney(discountAmount);
        RecalculateTotal();
    }

    public void ChangeStatus(OrderStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        }

        if (Status == newStatus)
        {
            return;
        }

        var allowed = Status switch
        {
            OrderStatus.Pending =>
                newStatus is OrderStatus.Processing or OrderStatus.Cancelled,

            OrderStatus.Processing =>
                newStatus is OrderStatus.InDelivery or OrderStatus.Cancelled,

            OrderStatus.InDelivery =>
                newStatus == OrderStatus.Completed,

            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException(
                $"Cannot change order status from {Status} to {newStatus}.");
        }

        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void RecalculateTotal()
    {
        TotalAmount = RoundMoney(
            Subtotal + DeliveryFee - DiscountAmount);

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static decimal RoundMoney(decimal value)
    {
        return decimal.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}