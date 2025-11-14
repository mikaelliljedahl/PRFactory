using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepo,
        ITicketRepository ticketRepo,
        ILogger<NotificationService> logger)
    {
        _notificationRepo = notificationRepo;
        _ticketRepo = ticketRepo;
        _logger = logger;
    }

    public async Task NotifyReviewerAssignedAsync(Guid reviewerId, Guid ticketId, bool isRequired)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            _logger.LogWarning("Cannot create notification: Ticket {TicketId} not found", ticketId);
            return;
        }

        var requiredText = isRequired ? "required" : "optional";
        var notification = Notification.Create(
            userId: reviewerId,
            type: NotificationType.ReviewerAssigned,
            ticketId: ticketId,
            title: $"You've been assigned as a {requiredText} reviewer",
            message: $"You have been assigned as a {requiredText} reviewer for plan: {ticket.Title}",
            actionUrl: $"/tickets/{ticketId}");

        await _notificationRepo.CreateAsync(notification);
    }

    public async Task NotifyMentionedInCommentAsync(
        List<Guid> mentionedUserIds,
        Guid ticketId,
        Guid commentId,
        string commentAuthor)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            _logger.LogWarning("Cannot create notification: Ticket {TicketId} not found", ticketId);
            return;
        }

        var notifications = mentionedUserIds.Select(userId =>
            Notification.Create(
                userId: userId,
                type: NotificationType.MentionedInComment,
                ticketId: ticketId,
                relatedEntityId: commentId,
                title: $"{commentAuthor} mentioned you in a comment",
                message: $"{commentAuthor} mentioned you in a comment on plan: {ticket.Title}",
                actionUrl: $"/tickets/{ticketId}#comment-{commentId}"))
            .ToList();

        await _notificationRepo.CreateManyAsync(notifications);
    }

    public async Task NotifyPlanApprovedAsync(Guid ticketId, List<Guid> reviewerIds)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            _logger.LogWarning("Cannot create notification: Ticket {TicketId} not found", ticketId);
            return;
        }

        var notifications = reviewerIds.Select(userId =>
            Notification.Create(
                userId: userId,
                type: NotificationType.PlanApproved,
                ticketId: ticketId,
                title: "Plan has been approved",
                message: $"The plan for '{ticket.Title}' has been approved and is moving to implementation.",
                actionUrl: $"/tickets/{ticketId}"))
            .ToList();

        await _notificationRepo.CreateManyAsync(notifications);
    }

    public async Task NotifyPlanRejectedAsync(Guid ticketId, List<Guid> reviewerIds, string reason)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            _logger.LogWarning("Cannot create notification: Ticket {TicketId} not found", ticketId);
            return;
        }

        var notifications = reviewerIds.Select(userId =>
            Notification.Create(
                userId: userId,
                type: NotificationType.PlanRejected,
                ticketId: ticketId,
                title: "Plan has been rejected",
                message: $"The plan for '{ticket.Title}' has been rejected. Reason: {reason}",
                actionUrl: $"/tickets/{ticketId}"))
            .ToList();

        await _notificationRepo.CreateManyAsync(notifications);
    }

    public async Task NotifyCommentReplyAsync(
        Guid originalAuthorId,
        Guid ticketId,
        Guid commentId,
        string replyAuthor)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            _logger.LogWarning("Cannot create notification: Ticket {TicketId} not found", ticketId);
            return;
        }

        var notification = Notification.Create(
            userId: originalAuthorId,
            type: NotificationType.CommentReply,
            ticketId: ticketId,
            relatedEntityId: commentId,
            title: $"{replyAuthor} replied to your comment",
            message: $"{replyAuthor} replied to your comment on plan: {ticket.Title}",
            actionUrl: $"/tickets/{ticketId}#comment-{commentId}");

        await _notificationRepo.CreateAsync(notification);
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(Guid userId, int limit = 50)
    {
        var notifications = await _notificationRepo.GetByUserIdAsync(userId, limit);
        return notifications.Select(MapToDto).ToList();
    }

    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(Guid userId)
    {
        var notifications = await _notificationRepo.GetUnreadByUserIdAsync(userId);
        return notifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _notificationRepo.GetUnreadCountAsync(userId);
    }

    public async Task<List<NotificationDto>> GetRecentNotificationsAsync(Guid userId, int limit = 10)
    {
        var notifications = await _notificationRepo.GetRecentAsync(userId, limit);
        return notifications.Select(MapToDto).ToList();
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type.ToString(),
            TicketId = notification.TicketId,
            TicketTitle = notification.Ticket?.Title ?? "Unknown",
            RelatedEntityId = notification.RelatedEntityId,
            Title = notification.Title,
            Message = notification.Message,
            ActionUrl = notification.ActionUrl,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _notificationRepo.GetByIdAsync(notificationId);
        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found", notificationId);
            return;
        }

        notification.MarkAsRead();
        await _notificationRepo.UpdateAsync(notification);
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await _notificationRepo.MarkAllAsReadAsync(userId);
    }
}
