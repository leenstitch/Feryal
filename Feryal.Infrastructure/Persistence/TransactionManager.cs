using Feryal.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Feryal.Infrastructure.Persistence.Migrations;
namespace Feryal.Infrastructure.Persistence;

public class TransactionManager
    : ITransactionManager
{
    private readonly ApplicationDbContext _context;

    public TransactionManager(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                isolationLevel,
                cancellationToken);

        try
        {
            var result = await action(cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                isolationLevel,
                cancellationToken);

        try
        {
            await action(cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }
}