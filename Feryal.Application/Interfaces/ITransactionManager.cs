using System.Data;

namespace Feryal.Application.Interfaces;

// Defines the contract for executing operations within database transactions.
public interface ITransactionManager
{
    // Executes an asynchronous operation that returns a result within a transaction using the specified isolation level.
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken);

    // Executes an asynchronous operation without a return value within a transaction using the specified isolation level.
    Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken);
}