
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly ApplicationDbContext _context;

    public OutboxMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Add(OutboxMessage message)
    {
        _context.OutboxMessages.Add(message);
    }

    public async Task<List<OutboxMessage>> GetUnprocessedAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "Batch size must be greater than zero.");
        }

        return await _context.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}