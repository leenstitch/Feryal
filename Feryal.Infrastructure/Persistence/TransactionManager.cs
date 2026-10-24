using Feryal.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Feryal.Infrastructure.Persistence.Migrations;

namespace Feryal.Infrastructure.Persistence;

// Manages database transactions to ensure operations are committed or rolled back as a unit.
public class TransactionManager
    : ITransactionManager
{
    private readonly ApplicationDbContext _context;

    // Inject the database context used to manage transactions.
    public TransactionManager(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // Executes an operation within a transaction and returns its result.
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        // Begin a transaction using the requested isolation level.
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                isolationLevel,
                cancellationToken);

        try
        {
            // Execute the operation within the active transaction.
            var result = await action(cancellationToken);

            // Commit the transaction if the operation succeeds.
            await transaction.CommitAsync(
                cancellationToken);

            return result;
        }
        catch
        {
            // Roll back the transaction if the operation fails.
            await transaction.RollbackAsync(
                cancellationToken);

            // Propagate the original exception to the caller.
            throw;
        }
    }

    // Executes an operation within a transaction when no return value is required.
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        // Begin a transaction using the requested isolation level.
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                isolationLevel,
                cancellationToken);

        try
        {
            // Execute the operation within the active transaction.
            await action(cancellationToken);

            // Commit the transaction if the operation succeeds.
            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            // Roll back the transaction if the operation fails.
            await transaction.RollbackAsync(
                cancellationToken);

            // Propagate the original exception to the caller.
            throw;
        }
    }
}