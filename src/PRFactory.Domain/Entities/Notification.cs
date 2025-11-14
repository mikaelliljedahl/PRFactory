namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a notification for a user
/// </summary>
public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? ActionUrl { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public Ticket Ticket { get; private set; } = null!;

    private Notification() { } // EF Core

    public static Notification Create(
        Guid userId,
        NotificationType type,
        Guid ticketId,
        string title,
        string message,
        string? actionUrl = null,
        Guid? relatedEntityId = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            TicketId = ticketId,
            RelatedEntityId = relatedEntityId,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

    public void MarkAsUnread()
    {
        IsRead = false;
        ReadAt = null;
    }
}

/// <summary>
/// Types of notifications
/// </summary>
public enum NotificationType
{
    ReviewerAssigned = 1,
    MentionedInComment = 2,
    PlanApproved = 3,
    PlanRejected = 4,
    CommentReply = 5,
    ReviewCompleted = 6
}
