namespace Feryal.Application.Interfaces;

// Defines the contract for saving pending changes to the database.
public interface IUnitOfWork
{
    // Saves all pending changes and returns the number of affected database entries.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}