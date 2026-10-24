
using Feryal.Domain.Entities;

namespace Feryal.Application.Interfaces;

public interface IOutboxMessageRepository
{
    void Add(OutboxMessage message);

    Task<List<OutboxMessage>> GetUnprocessedAsync(
        int batchSize,
        CancellationToken cancellationToken);
}