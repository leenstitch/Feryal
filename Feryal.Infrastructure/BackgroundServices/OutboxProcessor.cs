
using System.Text.Json;
using Feryal.Application.Events.Cart;
using Feryal.Application.Events.Categories;
using Feryal.Application.Events.Delivery;
using Feryal.Application.Events.Orders;
using Feryal.Application.Events.ProductGroups;
using Feryal.Application.Events.Products;
using Feryal.Application.Events.Wishlist;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Feryal.Infrastructure.BackgroundServices;

public sealed class OutboxProcessor : BackgroundService
{
    private const int BatchSize = 20;

    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while processing outbox messages.");
            }

            try
            {
                await Task.Delay(
                    PollingInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var services = scope.ServiceProvider;

        var outboxRepository =
            services.GetRequiredService<IOutboxMessageRepository>();

        var categoryCacheService =
            services.GetRequiredService<ICategoryCacheService>();

        var productGroupCacheService =
            services.GetRequiredService<IProductGroupCacheService>();

        var productCacheService =
            services.GetRequiredService<IProductCacheService>();

        var deliverySettingsCacheService =
            services.GetRequiredService<IDeliverySettingsCacheService>();

        var wishlistCacheService =
            services.GetRequiredService<IWishlistCacheService>();

        var cartCacheService =
            services.GetRequiredService<ICartCacheService>();

        var unitOfWork =
            services.GetRequiredService<IUnitOfWork>();
        var orderCacheService =
    services.GetRequiredService<IOrderCacheService>();
        var messages = await outboxRepository.GetUnprocessedAsync(
            BatchSize,
            cancellationToken);

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ProcessMessageAsync(
                    message,
                    categoryCacheService,
                    productGroupCacheService,
                    productCacheService,
                    deliverySettingsCacheService,
                    wishlistCacheService,
                    cartCacheService,
                    orderCacheService,
                    cancellationToken);

                message.MarkAsProcessed();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var error = exception.Message;

                if (error.Length > 2000)
                {
                    error = error[..2000];
                }

                message.RegisterFailure(error);

                _logger.LogError(
                    exception,
                    "Failed to process outbox message {MessageId}. Retry count: {RetryCount}",
                    message.Id,
                    message.RetryCount);
            }

            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static async Task ProcessMessageAsync(
        OutboxMessage message,
        ICategoryCacheService categoryCacheService,
        IProductGroupCacheService productGroupCacheService,
        IProductCacheService productCacheService,
        IDeliverySettingsCacheService deliverySettingsCacheService,
        IWishlistCacheService wishlistCacheService,
        ICartCacheService cartCacheService,
        IOrderCacheService orderCacheService,
        CancellationToken cancellationToken)
    {
        if (message.EventType ==
            typeof(CategoryCacheInvalidationRequested).FullName)
        {
            var cacheEvent =
                JsonSerializer.Deserialize<CategoryCacheInvalidationRequested>(
                    message.Payload);

            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the category cache invalidation event.");
            }

            if (cacheEvent.CategoryId.HasValue)
            {
                await categoryCacheService.InvalidateCategoryAsync(
                    cacheEvent.CategoryId.Value,
                    cancellationToken);
            }
            else
            {
                await categoryCacheService.InvalidateListsAsync(
                    cancellationToken);
            }

            return;
        }

        if (message.EventType ==
            typeof(ProductGroupCacheInvalidationRequested).FullName)
        {
            var cacheEvent =
                JsonSerializer.Deserialize<ProductGroupCacheInvalidationRequested>(
                    message.Payload);

            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the product group cache invalidation event.");
            }

            if (cacheEvent.ProductGroupId.HasValue)
            {
                await productGroupCacheService.InvalidateProductGroupAsync(
                    cacheEvent.ProductGroupId.Value,
                    cancellationToken);
            }
            else
            {
                await productGroupCacheService.InvalidateListsAsync(
                    cancellationToken);
            }

            return;
        }

        if (message.EventType ==
            typeof(ProductCacheInvalidationRequested).FullName)
        {
            var cacheEvent =
                JsonSerializer.Deserialize<ProductCacheInvalidationRequested>(
                    message.Payload);

            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the product cache invalidation event.");
            }

            if (cacheEvent.ProductId.HasValue)
            {
                await productCacheService.InvalidateProductAsync(
                    cacheEvent.ProductId.Value,
                    cancellationToken);
            }
            else
            {
                await productCacheService.InvalidateListsAsync(
                    cancellationToken);
            }

            return;
        }

        if (message.EventType ==
            typeof(DeliverySettingsCacheInvalidationRequested).FullName)
        {
            var cacheEvent =
                JsonSerializer.Deserialize<DeliverySettingsCacheInvalidationRequested>(
                    message.Payload);

            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the delivery cache invalidation event.");
            }

            if (cacheEvent.DeliveryCityId.HasValue)
            {
                await deliverySettingsCacheService.InvalidateCityAsync(
                    cacheEvent.DeliveryCityId.Value,
                    cancellationToken);
            }

            if (cacheEvent.PickupPointId.HasValue)
            {
                await deliverySettingsCacheService.InvalidatePickupPointAsync(
                    cacheEvent.PickupPointId.Value,
                    cancellationToken);
            }

            if (!cacheEvent.DeliveryCityId.HasValue &&
                !cacheEvent.PickupPointId.HasValue)
            {
                await deliverySettingsCacheService.InvalidateListsAsync(
                    cancellationToken);
            }

            return;
        }

        if (message.EventType ==
            typeof(WishlistCacheInvalidationRequested).FullName)
        {
            var cacheEvent =
                JsonSerializer.Deserialize<WishlistCacheInvalidationRequested>(
                    message.Payload);

            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the wishlist cache invalidation event.");
            }

            if (cacheEvent.UserId.HasValue)
            {
                await wishlistCacheService.InvalidateUserAsync(
                    cacheEvent.UserId.Value,
                    cancellationToken);
            }
            else
            {
                await wishlistCacheService.InvalidateAllAsync(
                    cancellationToken);
            }

            return;
        }
        if (message.EventType ==
    typeof(OrderCacheInvalidationRequested).FullName)
        {
            var cacheEvent =
                JsonSerializer.Deserialize<OrderCacheInvalidationRequested>(
                    message.Payload);

            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the order cache invalidation event.");
            }

            await orderCacheService.InvalidateUserAsync(
                cacheEvent.UserId,
                cancellationToken);

            await orderCacheService.InvalidateAdminAsync(
                cancellationToken);

            return;
        }
        if (message.EventType ==
            typeof(CartCacheInvalidationRequested).FullName)
        {
            var cacheEvent =
                JsonSerializer.Deserialize<CartCacheInvalidationRequested>(
                    message.Payload);

            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the cart cache invalidation event.");
            }

            await cartCacheService.InvalidateUserAsync(
                cacheEvent.UserId,
                cancellationToken);

            return;
        }

        throw new NotSupportedException(
            $"Unsupported outbox event type: {message.EventType}");
    }
}
