namespace Feryal.Application.Interfaces;

// Defines the generic contract for common database operations on entities.
public interface IRepository<T> where T : class
{
    // Retrieves an entity by its ID, returning null if it does not exist.
    Task<T?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    // Adds a new entity to the database.
    Task AddAsync(
        T entity,
        CancellationToken cancellationToken);

    // Marks an existing entity as modified.
    void Update(T entity);

    // Marks an existing entity for deletion.
    void Delete(T entity);

    // Checks whether an entity with the specified ID exists.
    Task<bool> ExistsAsync(
        int id,
        CancellationToken cancellationToken);
}