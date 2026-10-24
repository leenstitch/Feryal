using System.Data;

namespace Feryal.Application.Interfaces;

public interface ITransactionManager
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken);

    Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken);
}