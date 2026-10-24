using Feryal.Domain.Enums;

namespace Feryal.Domain.Entities;

public class Notification
{
    private Notification()
    {
    }

    public Notification(
        int userId,
        int? relatedOrderId,
        string title,
        string message,
        NotificationType type)
    {
        if (userId <= 0)
            throw new ArgumentException(
                "User is required.",
                nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "Notification title is required.",
                nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException(
                "Notification message is required.",
                nameof(message));

        UserId = userId;
        RelatedOrderId = relatedOrderId;

        Title = title.Trim();
        Message = message.Trim();

        Type = type;

        IsRead = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int UserId { get; private set; }

    public int? RelatedOrderId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Message { get; private set; } = null!;

    public NotificationType Type { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}