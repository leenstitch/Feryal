using Feryal.Domain.Enums;

namespace Feryal.Application.DTOs.Orders;

// Represents a cart item displayed during checkout, including pricing, stock, and order eligibility.
public sealed record CheckoutItemResponse(
    int CartItemId,
    int ProductVariantId,
    string ProductName,
    string? VariantDescription,
    string? ImageUrl,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal FinalPrice,
    int Quantity,
    int? AvailableStock,
    decimal LineTotal,
    bool CanOrder
    );

// Represents a delivery city option available during checkout.
public sealed record DeliveryCityOptionResponse(
    int Id,
    string Name,
    decimal DeliveryFee);

// Represents a pickup point option available during checkout.
public sealed record PickupPointOptionResponse(
    int Id,
    string Name,
    string Address,
    decimal PickupFee);

// Represents the customer's saved delivery address.
public sealed record SavedDeliveryAddressResponse(
    int? DeliveryCityId,
    string? Area,
    string? Street,
    string? BuildingNumber,
    string? AdditionalDetails);

// Represents the customer's profile information used during checkout.
public sealed record CheckoutCustomerProfile(
    int Id,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    int? DeliveryCityId,
    string? Area,
    string? Street,
    string? BuildingNumber,
    string? AdditionalDetails);

// Represents the checkout response, including cart items, subtotal, customer details, and delivery options.
public sealed record CheckoutResponse(
    IReadOnlyList<CheckoutItemResponse> Items,
    decimal Subtotal,
    string? PhoneNumber,
    string FirstName,
    string LastName,
    SavedDeliveryAddressResponse SavedAddress,
    IReadOnlyList<DeliveryCityOptionResponse> DeliveryCities,
    IReadOnlyList<PickupPointOptionResponse> PickupPoints);

// Represents the response returned after creating a reservation.
public sealed record CreateReservationResponse(
    int ReservationId,
    string ReservationToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<ReservedItemResponse> Items);

// Represents a product variant and its reserved quantity.
public sealed record ReservedItemResponse(
    int ProductVariantId,
    int Quantity);

// Represents the current status and expiration details of a reservation, including its associated order ID if available.
public sealed record ReservationStatusResponse(
    int ReservationId,
    string Status,
    DateTimeOffset ExpiresAt,
    int? OrderId);

// Represents the information required to confirm an order.
public sealed class ConfirmOrderRequest
{
    // Gets or sets the ID of the reservation to be converted into an order.
    public int ReservationId { get; set; }

    // Gets or sets the customer's phone number for the order.
    public string PhoneNumber { get; set; } = string.Empty;

    // Gets or sets the selected delivery method.
    public DeliveryType DeliveryType { get; set; }

    // Gets or sets the ID of the selected delivery city.
    public int? DeliveryCityId { get; set; }

    // Gets or sets the delivery area.
    public string? DeliveryArea { get; set; }

    // Gets or sets the delivery street.
    public string? DeliveryStreet { get; set; }

    // Gets or sets the delivery building number.
    public string? DeliveryBuildingNumber { get; set; }

    // Gets or sets additional details about the delivery address.
    public string? DeliveryAdditionalDetails { get; set; }

    // Gets or sets the ID of the selected pickup point.
    public int? PickupPointId { get; set; }
}

// Represents an individual item included in an order.
public sealed record OrderItemResponse(
    int Id,
    int ProductVariantId,
    string ProductName,
    string? VariantDescription,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
    );

// Represents the complete details of an order, including customer, delivery, pricing, status, and items.
public sealed record OrderDetailsResponse(
    int Id,
    string OrderNumber,
    int UserId,
    string CustomerName,
    string CustomerPhoneNumber,
    DeliveryType DeliveryType,
    int? DeliveryCityId,

    string? DeliveryCityName,
    string? DeliveryArea,
    string? DeliveryStreet,
    string? DeliveryBuildingNumber,
    string? DeliveryAdditionalDetails,
    int? PickupPointId,
    string? PickupPointName,
    string? PickupPointAddress,
    DateTimeOffset OrderDate,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal DeliveryFee,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<OrderItemResponse> Items);

// Represents a paginated collection of order details.
public sealed record PagedOrdersResponse(
    IReadOnlyList<OrderDetailsResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

// Represents a request to update an order's status.
public sealed class UpdateOrderStatusRequest
{
    // Gets or sets the new status to assign to the order.
    public OrderStatus Status { get; set; }
}