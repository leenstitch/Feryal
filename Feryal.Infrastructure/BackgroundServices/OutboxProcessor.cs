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

// Processes outbox messages in the background to handle cache invalidation events reliably.
public sealed class OutboxProcessor : BackgroundService
{
    // Maximum number of outbox messages processed in a single batch.
    private const int BatchSize = 20;

    // Time interval between consecutive outbox processing attempts.
    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(5);

    // Creates dependency injection scopes for accessing scoped services.
    private readonly IServiceScopeFactory _scopeFactory;

    // Records errors that occur during outbox processing.
    private readonly ILogger<OutboxProcessor> _logger;

    // Initializes the processor with its required dependencies.
    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Runs the background processing loop until cancellation is requested.
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Continue processing outbox messages while the application is running.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Retrieve and process a batch of pending messages.
                await ProcessBatchAsync(stoppingToken);
            }
            // Exit the loop when the application is shutting down.
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            // Log unexpected errors without terminating the background service.
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while processing outbox messages.");
            }

            try
            {
                // Wait before checking for new pending messages.
                await Task.Delay(
                    PollingInterval,
                    stoppingToken);
            }
            // Stop waiting when cancellation is requested.
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    // Retrieves a batch of pending messages and processes each one individually.
    private async Task ProcessBatchAsync(
        CancellationToken cancellationToken)
    {
        // Create a scope to resolve services registered with scoped lifetimes.
        using var scope = _scopeFactory.CreateScope();

        // Access the dependency injection container for this scope.
        var services = scope.ServiceProvider;

        // Resolve the repository responsible for retrieving and managing outbox messages.
        var outboxRepository =
            services.GetRequiredService<IOutboxMessageRepository>();

        // Resolve the service responsible for category cache invalidation.
        var categoryCacheService =
            services.GetRequiredService<ICategoryCacheService>();

        // Resolve the service responsible for product group cache invalidation.
        var productGroupCacheService =
            services.GetRequiredService<IProductGroupCacheService>();

        // Resolve the service responsible for product cache invalidation.
        var productCacheService =
            services.GetRequiredService<IProductCacheService>();

        // Resolve the service responsible for delivery settings cache invalidation.
        var deliverySettingsCacheService =
            services.GetRequiredService<IDeliverySettingsCacheService>();

        // Resolve the service responsible for wishlist cache invalidation.
        var wishlistCacheService =
            services.GetRequiredService<IWishlistCacheService>();

        // Resolve the service responsible for cart cache invalidation.
        var cartCacheService =
            services.GetRequiredService<ICartCacheService>();

        // Resolve the unit of work used to persist message processing results.
        var unitOfWork =
            services.GetRequiredService<IUnitOfWork>();

        // Resolve the service responsible for order cache invalidation.
        var orderCacheService =
    services.GetRequiredService<IOrderCacheService>();

        // Retrieve a limited batch of messages that have not been processed yet.
        var messages = await outboxRepository.GetUnprocessedAsync(
            BatchSize,
            cancellationToken);

        // Process each message independently so one failure does not stop the entire batch.
        foreach (var message in messages)
        {
            // Stop processing promptly if cancellation has been requested.
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Dispatch the message to the appropriate event handler.
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

                // Mark the message as successfully processed.
                message.MarkAsProcessed();
            }
            // Propagate cancellation so the background service can stop.
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            // Record the failure and log the error for troubleshooting and retries.
            catch (Exception exception)
            {
                // Store the error message without exceeding the maximum allowed length.
                var error = exception.Message;

                if (error.Length > 2000)
                {
                    error = error[..2000];
                }

                // Register the failed processing attempt on the outbox message.
                message.RegisterFailure(error);

                _logger.LogError(
                    exception,
                    "Failed to process outbox message {MessageId}. Retry count: {RetryCount}",
                    message.Id,
                    message.RetryCount);
            }

            // Persist the updated processing status or failure information.
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
    }

    // Deserializes an outbox message and dispatches it to the matching cache service.
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
        // Handle category cache invalidation events.
        if (message.EventType ==
            typeof(CategoryCacheInvalidationRequested).FullName)
        {
            // Deserialize the stored JSON payload into a category cache event.
            var cacheEvent =
                JsonSerializer.Deserialize<CategoryCacheInvalidationRequested>(
                    message.Payload);

            // Fail processing if the event payload cannot be deserialized.
            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the category cache invalidation event.");
            }

            // Invalidate one category when its ID is provided.
            if (cacheEvent.CategoryId.HasValue)
            {
                await categoryCacheService.InvalidateCategoryAsync(
                    cacheEvent.CategoryId.Value,
                    cancellationToken);
            }
            // Otherwise, invalidate the category lists.
            else
            {
                await categoryCacheService.InvalidateListsAsync(
                    cancellationToken);
            }

            return;
        }

        // Handle product group cache invalidation events.
        if (message.EventType ==
            typeof(ProductGroupCacheInvalidationRequested).FullName)
        {
            // Deserialize the stored JSON payload into a product group cache event.
            var cacheEvent =
                JsonSerializer.Deserialize<ProductGroupCacheInvalidationRequested>(
                    message.Payload);

            // Fail processing if the event payload cannot be deserialized.
            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the product group cache invalidation event.");
            }

            // Invalidate one product group when its ID is provided.
            if (cacheEvent.ProductGroupId.HasValue)
            {
                await productGroupCacheService.InvalidateProductGroupAsync(
                    cacheEvent.ProductGroupId.Value,
                    cancellationToken);
            }
            // Otherwise, invalidate the product group lists.
            else
            {
                await productGroupCacheService.InvalidateListsAsync(
                    cancellationToken);
            }

            return;
        }

        // Handle product cache invalidation events.
        if (message.EventType ==
            typeof(ProductCacheInvalidationRequested).FullName)
        {
            // Deserialize the stored JSON payload into a product cache event.
            var cacheEvent =
                JsonSerializer.Deserialize<ProductCacheInvalidationRequested>(
                    message.Payload);

            // Fail processing if the event payload cannot be deserialized.
            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the product cache invalidation event.");
            }

            // Invalidate one product when its ID is provided.
            if (cacheEvent.ProductId.HasValue)
            {
                await productCacheService.InvalidateProductAsync(
                    cacheEvent.ProductId.Value,
                    cancellationToken);
            }
            // Otherwise, invalidate the product lists.
            else
            {
                await productCacheService.InvalidateListsAsync(
                    cancellationToken);
            }

            return;
        }

        // Handle delivery settings cache invalidation events.
        if (message.EventType ==
            typeof(DeliverySettingsCacheInvalidationRequested).FullName)
        {
            // Deserialize the stored JSON payload into a delivery settings cache event.
            var cacheEvent =
                JsonSerializer.Deserialize<DeliverySettingsCacheInvalidationRequested>(
                    message.Payload);

            // Fail processing if the event payload cannot be deserialized.
            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the delivery cache invalidation event.");
            }

            // Invalidate the specified delivery city when its ID is provided.
            if (cacheEvent.DeliveryCityId.HasValue)
            {
                await deliverySettingsCacheService.InvalidateCityAsync(
                    cacheEvent.DeliveryCityId.Value,
                    cancellationToken);
            }

            // Invalidate the specified pickup point when its ID is provided.
            if (cacheEvent.PickupPointId.HasValue)
            {
                await deliverySettingsCacheService.InvalidatePickupPointAsync(
                    cacheEvent.PickupPointId.Value,
                    cancellationToken);
            }

            // Invalidate all delivery settings lists when no specific ID is provided.
            if (!cacheEvent.DeliveryCityId.HasValue &&
                !cacheEvent.PickupPointId.HasValue)
            {
                await deliverySettingsCacheService.InvalidateListsAsync(
                    cancellationToken);
            }

            return;
        }

        // Handle wishlist cache invalidation events.
        if (message.EventType ==
            typeof(WishlistCacheInvalidationRequested).FullName)
        {
            // Deserialize the stored JSON payload into a wishlist cache event.
            var cacheEvent =
                JsonSerializer.Deserialize<WishlistCacheInvalidationRequested>(
                    message.Payload);

            // Fail processing if the event payload cannot be deserialized.
            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the wishlist cache invalidation event.");
            }

            // Invalidate one user's wishlist when the user ID is provided.
            if (cacheEvent.UserId.HasValue)
            {
                await wishlistCacheService.InvalidateUserAsync(
                    cacheEvent.UserId.Value,
                    cancellationToken);
            }
            // Otherwise, invalidate all wishlist caches.
            else
            {
                await wishlistCacheService.InvalidateAllAsync(
                    cancellationToken);
            }

            return;
        }

        // Handle order cache invalidation events.
        if (message.EventType ==
    typeof(OrderCacheInvalidationRequested).FullName)
        {
            // Deserialize the stored JSON payload into an order cache event.
            var cacheEvent =
                JsonSerializer.Deserialize<OrderCacheInvalidationRequested>(
                    message.Payload);

            // Fail processing if the event payload cannot be deserialized.
            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the order cache invalidation event.");
            }

            // Invalidate the order cache for the affected user.
            await orderCacheService.InvalidateUserAsync(
                cacheEvent.UserId,
                cancellationToken);

            // Invalidate the admin order cache to keep administrative views up to date.
            await orderCacheService.InvalidateAdminAsync(
                cancellationToken);

            return;
        }

        // Handle cart cache invalidation events.
        if (message.EventType ==
            typeof(CartCacheInvalidationRequested).FullName)
        {
            // Deserialize the stored JSON payload into a cart cache event.
            var cacheEvent =
                JsonSerializer.Deserialize<CartCacheInvalidationRequested>(
                    message.Payload);

            // Fail processing if the event payload cannot be deserialized.
            if (cacheEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize the cart cache invalidation event.");
            }

            // Invalidate the cart cache for the affected user.
            await cartCacheService.InvalidateUserAsync(
                cacheEvent.UserId,
                cancellationToken);

            return;
        }

        // Reject event types that do not have a registered handler.
        throw new NotSupportedException(
            $"Unsupported outbox event type: {message.EventType}");
    }
}