using Feryal.Domain.Enums;

namespace Feryal.Application.DTOs.Orders;

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

public sealed record DeliveryCityOptionResponse(
    int Id,
    string Name,
    decimal DeliveryFee);

public sealed record PickupPointOptionResponse(
    int Id,
    string Name,
    string Address,
    decimal PickupFee);

public sealed record SavedDeliveryAddressResponse(
    int? DeliveryCityId,
    string? Area,
    string? Street,
    string? BuildingNumber,
    string? AdditionalDetails);

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

public sealed record CheckoutResponse(
    IReadOnlyList<CheckoutItemResponse> Items,
    decimal Subtotal,
    string? PhoneNumber,
    string FirstName,
    string LastName,
    SavedDeliveryAddressResponse SavedAddress,
    IReadOnlyList<DeliveryCityOptionResponse> DeliveryCities,
    IReadOnlyList<PickupPointOptionResponse> PickupPoints);

public sealed record CreateReservationResponse(
    int ReservationId,
    string ReservationToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<ReservedItemResponse> Items);

public sealed record ReservedItemResponse(
    int ProductVariantId,
    int Quantity);

public sealed record ReservationStatusResponse(
    int ReservationId,
    string Status,
    DateTimeOffset ExpiresAt,
    int? OrderId);

public sealed class ConfirmOrderRequest
{
    public int ReservationId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public DeliveryType DeliveryType { get; set; }

    public int? DeliveryCityId { get; set; }

    public string? DeliveryArea { get; set; }

    public string? DeliveryStreet { get; set; }

    public string? DeliveryBuildingNumber { get; set; }

    public string? DeliveryAdditionalDetails { get; set; }

    public int? PickupPointId { get; set; }
}

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

public sealed record PagedOrdersResponse(
    IReadOnlyList<OrderDetailsResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}