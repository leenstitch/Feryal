
using Feryal.Application.Interfaces;
using Feryal.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Feryal.Infrastructure.BackgroundServices;

public sealed class InventoryReservationExpiryService : BackgroundService
{
    private const int BatchSize = 100;

    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventoryReservationExpiryService> _logger;

    public InventoryReservationExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<InventoryReservationExpiryService> logger)
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
                await ProcessExpiredReservationsAsync(stoppingToken);
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
                    "An error occurred while expiring inventory reservations.");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessExpiredReservationsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<IOrderRepository>();

        var unitOfWork = scope.ServiceProvider
            .GetRequiredService<IUnitOfWork>();

        var transactionManager = scope.ServiceProvider
            .GetRequiredService<ITransactionManager>();

        var candidates = await repository.GetExpiredReservationsAsync(
            DateTimeOffset.UtcNow,
            BatchSize,
            cancellationToken);

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await transactionManager.ExecuteAsync(
                    async token =>
                    {
                        bool changed =
                            await repository.TryChangeReservationStatusAsync(
                                candidate.Id,
                                InventoryReservationStatus.Active,
                                InventoryReservationStatus.Expired,
                                DateTimeOffset.UtcNow,
                                token);

                        if (!changed)
                        {
                            return;
                        }

                        var reservation =
                            await repository.GetReservationAsync(
                                candidate.Id,
                                candidate.UserId,
                                token);

                        if (reservation is null)
                        {
                            throw new InvalidOperationException(
                                $"Reservation {candidate.Id} could not be loaded.");
                        }

                        foreach (var item in reservation.Items
                                     .OrderBy(x => x.ProductVariantId))
                        {
                            bool released =
                                await repository.TryReleaseReservedStockAsync(
                                    item.ProductVariantId,
                                    item.Quantity,
                                    token);

                            if (!released)
                            {
                                throw new InvalidOperationException(
                                    $"Could not release reserved stock for variant {item.ProductVariantId}.");
                            }
                        }

                        await unitOfWork.SaveChangesAsync(token);
                    },
                    System.Data.IsolationLevel.ReadCommitted,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
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
