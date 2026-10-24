using Feryal.Application.Interfaces;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public class Repository<T> : IRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _dbSet.FindAsync(
            new object?[] { id },
            cancellationToken);
    }

    public async Task AddAsync(
        T entity,
        CancellationToken cancellationToken)
    {
        await _dbSet.AddAsync(
            entity,
            cancellationToken);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<bool> ExistsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FindAsync(
            new object?[] { id },
            cancellationToken);

        return entity is not null;
    }
}