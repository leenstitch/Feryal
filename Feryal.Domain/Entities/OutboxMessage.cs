
namespace Feryal.Domain.Entities;

public sealed class OutboxMessage
{
    public long Id { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public int RetryCount { get; private set; }

    public string? Error { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(
        string eventType,
        string payload)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException(
                "Event type is required.",
                nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException(
                "Payload is required.",
                nameof(payload));
        }

        EventType = eventType.Trim();
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        if (ProcessedAt.HasValue)
        {
            return;
        }

        ProcessedAt = DateTime.UtcNow;
        Error = null;
    }

    public void RegisterFailure(string error)
    {
        if (ProcessedAt.HasValue)
        {
            throw new InvalidOperationException(
                "A processed message cannot be marked as failed.");
        }

        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException(
                "Error message is required.",
                nameof(error));
        }

        RetryCount++;
        Error = error;
    }
}