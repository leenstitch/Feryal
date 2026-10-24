using Feryal.Application.Interfaces;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Provides generic database operations for entities of type T.
public class Repository<T> : IRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    // Inject the database context and initialize the DbSet for the specified entity type.
    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // Retrieves an entity by its primary key.
    public async Task<T?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _dbSet.FindAsync(
            new object?[] { id },
            cancellationToken);
    }

    // Adds a new entity to the database context.
    // Changes are persisted when SaveChangesAsync is called.
    public async Task AddAsync(
        T entity,
        CancellationToken cancellationToken)
    {
        await _dbSet.AddAsync(
            entity,
            cancellationToken);
    }

    // Marks an existing entity as modified so its changes can be saved.
    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    // Marks an entity for deletion from the database.
    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    // Checks whether an entity with the specified primary key exists.
    public async Task<bool> ExistsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FindAsync(
            new object?[] { id },
            cancellationToken);

        // Return true if the entity was found; otherwise, return false.
        return entity is not null;
    }
}