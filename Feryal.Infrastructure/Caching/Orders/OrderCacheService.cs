using Feryal.Application.DTOs.Orders;
using Feryal.Application.Interfaces;
using Feryal.Application.Services;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Services;

// Provides hybrid caching for customer and admin order queries.
public sealed class OrderCacheService : IOrderCacheService
{
    // Handles cache storage, retrieval, and invalidation.
    private readonly HybridCache _cache;

    // Retrieves order data from the database when the cache misses.
    private readonly IOrderRepository _repository;

    // Configures cache expiration using the shared order cache settings.
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = OrderCacheConstants.Expiration,
        LocalCacheExpiration = OrderCacheConstants.LocalExpiration
    };

    // Initializes the service with the hybrid cache and order repository.
    public OrderCacheService(
        HybridCache cache,
        IOrderRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    // Retrieves a paginated list of orders belonging to the specified user.
    public async Task<PagedOrdersResponse> GetMyOrdersAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Build a unique cache key for the user's requested page.
        string key =
            $"{OrderCacheConstants.UserKeyPrefix}{userId}" +
            $":page:{pageNumber}:size:{pageSize}";

        // Return the cached result or retrieve and cache the user's orders.
        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                // Retrieve the requested page of orders from the repository.
                var result = await _repository.GetMyOrdersAsync(
                    userId,
                    pageNumber,
                    pageSize,
                    token);

                // Convert the retrieved orders into the paginated response DTO.
                return MapPage(
                    result.Items,
                    pageNumber,
                    pageSize,
                    result.TotalCount);
            },
            CacheOptions,
            // Tag the cache entry with the user ID for targeted invalidation.
            new[] { $"{OrderCacheConstants.UserTagPrefix}{userId}" },
            cancellationToken);
    }

    // Retrieves a paginated list of orders for administrators using the supplied filters.
    public async Task<PagedOrdersResponse> GetAdminOrdersAsync(
        int pageNumber,
        int pageSize,
        OrderStatus? status,
        string? search,
        int? deliveryCityId,
        int? pickupPointId,
        CancellationToken cancellationToken)
    {
        // Normalize the search text to create a consistent cache key.
        string normalizedSearch =
            (search ?? string.Empty).Trim().ToUpperInvariant();

        // Include pagination and all filters in the key to distinguish query results.
        string key =
            $"{OrderCacheConstants.AdminKeyPrefix}" +
            $"page:{pageNumber}:size:{pageSize}" +
            $":status:{status}" +
            $":search:{normalizedSearch}" +
            $":city:{deliveryCityId}" +
            $":pickup:{pickupPointId}";

        // Return the cached result or retrieve and cache the filtered admin orders.
        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                // Retrieve orders matching the requested pagination and filters.
                var result = await _repository.GetAdminOrdersAsync(
                    pageNumber,
                    pageSize,
                    status,
                    search,
                    deliveryCityId,
                    pickupPointId,
                    token);

                // Convert the retrieved orders into the paginated response DTO.
                return MapPage(
                    result.Items,
                    pageNumber,
                    pageSize,
                    result.TotalCount);
            },
            CacheOptions,
            // Tag admin order entries so they can be invalidated together.
            new[] { OrderCacheConstants.AdminTag },
            cancellationToken);
    }

    // Invalidates all cached order queries associated with the specified user.
    public async Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        // Remove cache entries tagged with this user's order tag.
        await _cache.RemoveByTagAsync(
            $"{OrderCacheConstants.UserTagPrefix}{userId}",
            cancellationToken);
    }

    // Invalidates all cached order queries used by administrators.
    public async Task InvalidateAdminAsync(
        CancellationToken cancellationToken)
    {
        // Remove cache entries associated with the admin orders tag.
        await _cache.RemoveByTagAsync(
            OrderCacheConstants.AdminTag,
            cancellationToken);
    }

    // Maps a list of order entities into a paginated response.
    private static PagedOrdersResponse MapPage(
        List<Order> orders,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Convert each order entity into its response DTO.
        var items = orders.Select(MapOrder).ToList();

        // Calculate the total number of pages, returning zero when there are no orders.
        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        // Create the paginated response with its items and pagination metadata.
        return new PagedOrdersResponse(
            items,
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
    }

    // Maps an order entity and its items into an order details response.
    private static OrderDetailsResponse MapOrder(Order order)
    {
        // Convert each order item into its corresponding response DTO.
        var items = order.Items.Select(item =>
            new OrderItemResponse(
                item.Id,
                item.ProductVariantId,
                item.ProductName,
                item.VariantDescription,
                // Retrieve the primary image URL of the associated product, if available.
                item.ProductVariant?.Product?.Images
                    .FirstOrDefault(image => image.IsPrimary)?.ImageUrl,
                item.Quantity,
                item.UnitPrice,
                item.TotalPrice))
            .ToList();

        // Construct the order response with customer, delivery, pricing, status, and item details.
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