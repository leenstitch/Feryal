using Feryal.Domain.Entities;

// Defines the contract for managing outbox messages in the database.
public interface IOutboxMessageRepository
{
    // Adds a new outbox message to the database context.
    void Add(OutboxMessage message);

    // Retrieves a batch of outbox messages that have not yet been processed.
    Task<List<OutboxMessage>> GetUnprocessedAsync(
        int batchSize,
        CancellationToken cancellationToken);
}