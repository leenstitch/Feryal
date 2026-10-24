using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Provides database operations for managing outbox messages.
public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly ApplicationDbContext _context;

    // Inject the database context to access outbox messages.
    public OutboxMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Adds a new outbox message to the database context.
    // The changes are persisted when SaveChangesAsync is called.
    public void Add(OutboxMessage message)
    {
        _context.OutboxMessages.Add(message);
    }

    // Retrieves a batch of outbox messages that have not been processed.
    public async Task<List<OutboxMessage>> GetUnprocessedAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        // Ensure the requested batch size is greater than zero.
        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "Batch size must be greater than zero.");
        }

        // Retrieve the oldest unprocessed messages first.
        // Use the message ID as a secondary sort to ensure deterministic ordering.
        return await _context.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}