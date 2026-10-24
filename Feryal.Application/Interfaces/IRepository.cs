namespace Feryal.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task AddAsync(
        T entity,
        CancellationToken cancellationToken);

    void Update(T entity);

    void Delete(T entity);

    Task<bool> ExistsAsync(
        int id,
        CancellationToken cancellationToken);
}