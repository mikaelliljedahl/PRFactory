using PRFactory.Core.Application.DTOs;

namespace PRFactory.Core.Application.Services;

public interface INotificationService
{
    // Create notifications
    Task NotifyReviewerAssignedAsync(Guid reviewerId, Guid ticketId, bool isRequired);
    Task NotifyMentionedInCommentAsync(List<Guid> mentionedUserIds, Guid ticketId, Guid commentId, string commentAuthor);
    Task NotifyPlanApprovedAsync(Guid ticketId, List<Guid> reviewerIds);
    Task NotifyPlanRejectedAsync(Guid ticketId, List<Guid> reviewerIds, string reason);
    Task NotifyCommentReplyAsync(Guid originalAuthorId, Guid ticketId, Guid commentId, string replyAuthor);

    // Retrieve notifications
    Task<List<NotificationDto>> GetNotificationsAsync(Guid userId, int limit = 50);
    Task<List<NotificationDto>> GetUnreadNotificationsAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<List<NotificationDto>> GetRecentNotificationsAsync(Guid userId, int limit = 10);

    // Mark as read/unread
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
}
