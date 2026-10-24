
using Feryal.Application.DTOs.Orders;
using Feryal.Application.Interfaces;
using Feryal.Application.Services;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Services;

public sealed class OrderCacheService : IOrderCacheService
{
    private readonly HybridCache _cache;
    private readonly IOrderRepository _repository;

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = OrderCacheConstants.Expiration,
        LocalCacheExpiration = OrderCacheConstants.LocalExpiration
    };

    public OrderCacheService(
        HybridCache cache,
        IOrderRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<PagedOrdersResponse> GetMyOrdersAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        string key =
            $"{OrderCacheConstants.UserKeyPrefix}{userId}" +
            $":page:{pageNumber}:size:{pageSize}";

        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                var result = await _repository.GetMyOrdersAsync(
                    userId,
                    pageNumber,
                    pageSize,
                    token);

                return MapPage(
                    result.Items,
                    pageNumber,
                    pageSize,
                    result.TotalCount);
            },
            CacheOptions,
            new[] { $"{OrderCacheConstants.UserTagPrefix}{userId}" },
            cancellationToken);
    }

    public async Task<PagedOrdersResponse> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken)
    {
        string normalizedSearch =
            (search ?? string.Empty).Trim().ToUpperInvariant();

        string key =
            $"{OrderCacheConstants.AdminKeyPrefix}" +
            $"page:{pageNumber}:size:{pageSize}" +
            $":status:{status}" +
            $":search:{normalizedSearch}" +
            $":city:{deliveryCityId}" +
            $":pickup:{pickupPointId}";

        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                var result = await _repository.GetAdminOrdersAsync(
                    pageNumber,
                    pageSize,
                    status,
                    search,
                    deliveryCityId,
                    pickupPointId,
                    token);

                return MapPage(
                    result.Items,
                    pageNumber,
                    pageSize,
                    result.TotalCount);
            },
            CacheOptions,
            new[] { OrderCacheConstants.AdminTag },
            cancellationToken);
    }

    public async Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        await _cache.RemoveByTagAsync(
            $"{OrderCacheConstants.UserTagPrefix}{userId}",
            cancellationToken);
    }

    public async Task InvalidateAdminAsync(
        CancellationToken cancellationToken)
    {
        await _cache.RemoveByTagAsync(
            OrderCacheConstants.AdminTag,
            cancellationToken);
    }

    private static PagedOrdersResponse MapPage(
        List<Order> orders,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        var items = orders.Select(MapOrder).ToList();

        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedOrdersResponse(
            items,
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
    }

    private static OrderDetailsResponse MapOrder(Order order)
    {
        var items = order.Items.Select(item =>
            new OrderItemResponse(
                item.Id,
                item.ProductVariantId,
                item.ProductName,
                item.VariantDescription,
                item.ProductVariant?.Product?.Images
                    .FirstOrDefault(image => image.IsPrimary)?.ImageUrl,
                item.Quantity,
                item.UnitPrice,
                item.TotalPrice))
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
}
