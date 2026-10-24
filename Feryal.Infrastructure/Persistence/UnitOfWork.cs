using Feryal.Application.Interfaces;

namespace Feryal.Infrastructure.Persistence.Migrations;

// Implements the Unit of Work pattern to coordinate database changes.
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    // Inject the database context used to manage persistence.
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    // Saves all pending changes in the current database context.
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}