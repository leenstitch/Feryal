using Feryal.Application.Interfaces;
using Feryal.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Feryal.Infrastructure.BackgroundServices;

// Periodically checks for expired inventory reservations and releases their reserved stock.
public sealed class InventoryReservationExpiryService : BackgroundService
{
    // Maximum number of expired reservation candidates processed in one batch.
    private const int BatchSize = 100;

    // Time interval between consecutive reservation expiry checks.
    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(30);

    // Creates dependency injection scopes for accessing scoped services.
    private readonly IServiceScopeFactory _scopeFactory;

    // Records errors that occur during background processing.
    private readonly ILogger<InventoryReservationExpiryService> _logger;

    // Initializes the background service with its required dependencies.
    public InventoryReservationExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<InventoryReservationExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Runs the background process until the application requests cancellation.
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Continue checking for expired reservations while the service is running.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Find expired reservations and process them.
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            // Stop processing when the application shuts down.
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
                    "An error occurred while expiring inventory reservations.");
            }

            try
            {
                // Wait before starting the next expiry check.
                await Task.Delay(PollingInterval, stoppingToken);
            }
            // Exit the loop if the delay is cancelled during shutdown.
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    // Retrieves expired reservation candidates and processes each one independently.
    private async Task ProcessExpiredReservationsAsync(
        CancellationToken cancellationToken)
    {
        // Create a scope to resolve services registered with scoped lifetimes.
        using var scope = _scopeFactory.CreateScope();

        // Resolve the repository used to retrieve reservations and manage reserved stock.
        var repository = scope.ServiceProvider
            .GetRequiredService<IOrderRepository>();

        // Resolve the unit of work used to persist database changes.
        var unitOfWork = scope.ServiceProvider
            .GetRequiredService<IUnitOfWork>();

        // Resolve the transaction manager for atomic reservation processing.
        var transactionManager = scope.ServiceProvider
            .GetRequiredService<ITransactionManager>();

        // Retrieve a limited batch of reservations whose expiry time has passed.
        var candidates = await repository.GetExpiredReservationsAsync(
            DateTimeOffset.UtcNow,
            BatchSize,
            cancellationToken);

        // Process each expired reservation candidate.
        foreach (var candidate in candidates)
        {
            // Stop processing promptly if cancellation has been requested.
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Process the reservation status change and stock release atomically.
                await transactionManager.ExecuteAsync(
                    async token =>
                    {
                        // Change the reservation from Active to Expired only if its current status matches.
                        bool changed =
                            await repository.TryChangeReservationStatusAsync(
                                candidate.Id,
                                InventoryReservationStatus.Active,
                                InventoryReservationStatus.Expired,
                                DateTimeOffset.UtcNow,
                                token);

                        // Skip the candidate if its status could not be changed.
                        if (!changed)
                        {
                            return;
                        }

                        // Load the reservation and its items using the reservation and user IDs.
                        var reservation =
                            await repository.GetReservationAsync(
                                candidate.Id,
                                candidate.UserId,
                                token);

                        // Abort the transaction if the reservation cannot be loaded.
                        if (reservation is null)
                        {
                            throw new InvalidOperationException(
                                $"Reservation {candidate.Id} could not be loaded.");
                        }

                        // Process items in product variant ID order.
                        foreach (var item in reservation.Items
                                     .OrderBy(x => x.ProductVariantId))
                        {
                            // Release the quantity previously reserved for this variant.
                            bool released =
                                await repository.TryReleaseReservedStockAsync(
                                    item.ProductVariantId,
                                    item.Quantity,
                                    token);

                            // Abort the transaction if the reserved stock cannot be released.
                            if (!released)
                            {
                                throw new InvalidOperationException(
                                    $"Could not release reserved stock for variant {item.ProductVariantId}.");
                            }
                        }

                        // Persist the reservation and stock changes within the transaction.
                        await unitOfWork.SaveChangesAsync(token);
                    },
                    System.Data.IsolationLevel.ReadCommitted,
                    cancellationToken);
            }
            // Propagate cancellation so the background service can stop.
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            // Log the failure for this reservation and continue processing other candidates.
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to expire reservation {ReservationId}.",
                    candidate.Id);
            }
        }
    }
}