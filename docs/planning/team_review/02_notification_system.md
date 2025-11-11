# Implementation Plan: Notification System

**Feature:** In-app notification system for reviewer assignments and @mentions
**Priority:** P1
**Estimated Effort:** 3-4 days
**Dependencies:** Existing user and review infrastructure

---

## Overview

Create a notification system that alerts users when:
- They are assigned as a reviewer (required or optional)
- They are mentioned in a review comment (@mentions)
- A plan they're reviewing is approved or rejected
- A comment they authored receives a reply

Initially, this will be **in-app notifications only**. Future enhancements can add email/Slack/Teams integration.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│               Notification Sources                           │
│  - Reviewer Assignment                                       │
│  - @Mention in Comment                                       │
│  - Plan Approved/Rejected                                    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│             INotificationService                             │
│       (Create and manage notifications)                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│           Notification Repository                            │
│         (Persist notifications in database)                  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Blazor UI Components                            │
│  - NotificationBell (navbar)                                 │
│  - NotificationDropdown (list of recent)                     │
└─────────────────────────────────────────────────────────────┘
```

---

## Database Schema

### Notifications Table

```sql
CREATE TABLE Notifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL, -- Recipient user
    NotificationType NVARCHAR(50) NOT NULL, -- ReviewerAssigned, MentionedInComment, PlanApproved, etc.
    TicketId UNIQUEIDENTIFIER NOT NULL,
    RelatedEntityId UNIQUEIDENTIFIER NULL, -- CommentId, ReviewId, etc.
    Title NVARCHAR(255) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    ActionUrl NVARCHAR(500) NULL, -- Link to take action
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ReadAt DATETIME2 NULL,

    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Notifications_Tickets FOREIGN KEY (TicketId) REFERENCES Tickets(Id)
);

CREATE INDEX IX_Notifications_UserId_IsRead ON Notifications(UserId, IsRead);
CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt DESC);
```

**Fields:**
- `Id` - Unique notification ID
- `UserId` - Who receives this notification
- `NotificationType` - Type of notification (enum)
- `TicketId` - Related ticket
- `RelatedEntityId` - Optional ID of comment, review, etc.
- `Title` - Short title (e.g., "You were assigned as a reviewer")
- `Message` - Longer message with context
- `ActionUrl` - Where to navigate when clicked
- `IsRead` - Whether user has seen it
- `CreatedAt` - When created
- `ReadAt` - When marked as read

---

## Entity

**File:** `/src/PRFactory.Domain/Entities/Notification.cs`

```csharp
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
```

**Lines of Code:** ~75 lines

---

## Repository Interface

**File:** `/src/PRFactory.Domain/Interfaces/INotificationRepository.cs`

```csharp
namespace PRFactory.Domain.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid notificationId);
    Task<List<Notification>> GetByUserIdAsync(Guid userId, int limit = 50);
    Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<List<Notification>> GetRecentAsync(Guid userId, int limit = 10);
    Task CreateAsync(Notification notification);
    Task CreateManyAsync(List<Notification> notifications);
    Task UpdateAsync(Notification notification);
    Task DeleteAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
}
```

**Lines of Code:** ~20 lines

---

## Repository Implementation

**File:** `/src/PRFactory.Infrastructure/Persistence/Repositories/NotificationRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(
        ApplicationDbContext context,
        ILogger<NotificationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Notification?> GetByIdAsync(Guid notificationId)
    {
        return await _context.Notifications
            .Include(n => n.User)
            .Include(n => n.Ticket)
            .FirstOrDefaultAsync(n => n.Id == notificationId);
    }

    public async Task<List<Notification>> GetByUserIdAsync(Guid userId, int limit = 50)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<List<Notification>> GetRecentAsync(Guid userId, int limit = 10)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created notification {NotificationId} for user {UserId} of type {Type}",
            notification.Id, notification.UserId, notification.Type);
    }

    public async Task CreateManyAsync(List<Notification> notifications)
    {
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created {Count} notifications",
            notifications.Count);
    }

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unreadNotifications = await GetUnreadByUserIdAsync(userId);
        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Marked {Count} notifications as read for user {UserId}",
            unreadNotifications.Count, userId);
    }
}
```

**Lines of Code:** ~110 lines

---

## Application Service

**File:** `/src/PRFactory.Core/Application/Services/INotificationService.cs`

```csharp
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
```

**Lines of Code:** ~25 lines

---

**File:** `/src/PRFactory.Infrastructure/Application/NotificationService.cs`

```csharp
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using Microsoft.Extensions.Logging;

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
        return notifications.Select(NotificationDto.FromEntity).ToList();
    }

    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(Guid userId)
    {
        var notifications = await _notificationRepo.GetUnreadByUserIdAsync(userId);
        return notifications.Select(NotificationDto.FromEntity).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _notificationRepo.GetUnreadCountAsync(userId);
    }

    public async Task<List<NotificationDto>> GetRecentNotificationsAsync(Guid userId, int limit = 10)
    {
        var notifications = await _notificationRepo.GetRecentAsync(userId, limit);
        return notifications.Select(NotificationDto.FromEntity).ToList();
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
```

**Lines of Code:** ~190 lines

---

## DTO

**File:** `/src/PRFactory.Web/Models/NotificationDto.cs`

```csharp
using PRFactory.Domain.Entities;

namespace PRFactory.Web.Models;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketTitle { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public static NotificationDto FromEntity(Notification notification)
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
}
```

**Lines of Code:** ~40 lines

---

## Blazor Components

### NotificationBell Component

**File:** `/src/PRFactory.Web/Components/Notifications/NotificationBell.razor`

```razor
@namespace PRFactory.Web.Components.Notifications
@using PRFactory.Web.Models
@inject INotificationService NotificationService
@inject ICurrentUserService CurrentUserService
@implements IDisposable

<div class="dropdown">
    <button class="btn btn-link position-relative" type="button" id="notificationDropdown"
            data-bs-toggle="dropdown" aria-expanded="false" title="Notifications">
        <i class="bi bi-bell fs-5"></i>
        @if (unreadCount > 0)
        {
            <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
                @unreadCount
                <span class="visually-hidden">unread notifications</span>
            </span>
        }
    </button>
    <ul class="dropdown-menu dropdown-menu-end notification-dropdown" aria-labelledby="notificationDropdown">
        <li class="dropdown-header d-flex justify-content-between align-items-center">
            <span>Notifications</span>
            @if (unreadCount > 0)
            {
                <button class="btn btn-link btn-sm text-decoration-none" @onclick="MarkAllAsRead">
                    Mark all read
                </button>
            }
        </li>
        <li><hr class="dropdown-divider"></li>

        @if (notifications.Any())
        {
            @foreach (var notification in notifications)
            {
                <li>
                    <a class="dropdown-item @(!notification.IsRead ? "notification-unread" : "")"
                       href="@notification.ActionUrl" @onclick="() => MarkAsRead(notification.Id)">
                        <div class="d-flex">
                            <div class="flex-shrink-0 me-2">
                                <i class="bi bi-@GetNotificationIcon(notification.Type) fs-5 text-@GetNotificationColor(notification.Type)"></i>
                            </div>
                            <div class="flex-grow-1">
                                <div class="fw-bold">@notification.Title</div>
                                <div class="small text-muted">@notification.Message</div>
                                <div class="small text-muted">@FormatRelativeTime(notification.CreatedAt)</div>
                            </div>
                            @if (!notification.IsRead)
                            {
                                <div class="flex-shrink-0">
                                    <span class="badge bg-primary rounded-pill">New</span>
                                </div>
                            }
                        </div>
                    </a>
                </li>
            }
            <li><hr class="dropdown-divider"></li>
            <li>
                <a class="dropdown-item text-center small" href="/notifications">
                    View all notifications
                </a>
            </li>
        }
        else
        {
            <li class="dropdown-item text-center text-muted">
                <i class="bi bi-inbox me-2"></i>
                No notifications
            </li>
        }
    </ul>
</div>

<style>
    .notification-dropdown {
        min-width: 350px;
        max-width: 400px;
        max-height: 500px;
        overflow-y: auto;
    }

    .notification-unread {
        background-color: #f0f8ff;
    }

    .dropdown-item {
        white-space: normal;
        padding: 0.75rem 1rem;
    }

    .dropdown-item:hover {
        background-color: #f8f9fa;
    }
</style>
```

**Code-behind:** `NotificationBell.razor.cs`

```csharp
using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Models;
using System.Timers;

namespace PRFactory.Web.Components.Notifications;

public partial class NotificationBell : IDisposable
{
    [Inject]
    private INotificationService NotificationService { get; set; } = null!;

    [Inject]
    private ICurrentUserService CurrentUserService { get; set; } = null!;

    private List<NotificationDto> notifications = new();
    private int unreadCount = 0;
    private System.Timers.Timer? pollTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadNotifications();

        // Poll for new notifications every 30 seconds
        pollTimer = new System.Timers.Timer(30000);
        pollTimer.Elapsed += async (sender, e) => await PollNotifications();
        pollTimer.Start();
    }

    private async Task LoadNotifications()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        if (currentUser == null) return;

        notifications = await NotificationService.GetRecentNotificationsAsync(currentUser.Id, 10);
        unreadCount = await NotificationService.GetUnreadCountAsync(currentUser.Id);
        StateHasChanged();
    }

    private async Task PollNotifications()
    {
        await LoadNotifications();
    }

    private async Task MarkAsRead(Guid notificationId)
    {
        await NotificationService.MarkAsReadAsync(notificationId);
        await LoadNotifications();
    }

    private async Task MarkAllAsRead()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        if (currentUser == null) return;

        await NotificationService.MarkAllAsReadAsync(currentUser.Id);
        await LoadNotifications();
    }

    private string GetNotificationIcon(string type)
    {
        return type switch
        {
            "ReviewerAssigned" => "person-check",
            "MentionedInComment" => "at",
            "PlanApproved" => "check-circle",
            "PlanRejected" => "x-circle",
            "CommentReply" => "reply",
            _ => "bell"
        };
    }

    private string GetNotificationColor(string type)
    {
        return type switch
        {
            "ReviewerAssigned" => "primary",
            "MentionedInComment" => "info",
            "PlanApproved" => "success",
            "PlanRejected" => "warning",
            "CommentReply" => "info",
            _ => "secondary"
        };
    }

    private string FormatRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        return timeSpan switch
        {
            { TotalSeconds: < 60 } => "just now",
            { TotalMinutes: < 60 } => $"{(int)timeSpan.TotalMinutes}m ago",
            { TotalHours: < 24 } => $"{(int)timeSpan.TotalHours}h ago",
            { TotalDays: < 7 } => $"{(int)timeSpan.TotalDays}d ago",
            _ => dateTime.ToString("MMM d")
        };
    }

    public void Dispose()
    {
        pollTimer?.Stop();
        pollTimer?.Dispose();
    }
}
```

**Lines of Code:** ~95 lines (razor) + ~100 lines (code-behind) = ~195 lines

---

## Integration Points

### Trigger Notifications When Reviewers Assigned

**File to Modify:** `/src/PRFactory.Infrastructure/Application/PlanReviewService.cs`

In `AssignReviewersAsync()` method, add:

```csharp
private readonly INotificationService _notificationService;

// After creating PlanReview entities:
foreach (var review in allReviews)
{
    await _notificationService.NotifyReviewerAssignedAsync(
        review.ReviewerId,
        ticketId,
        review.IsRequired);
}
```

### Trigger Notifications When @Mentioned

**File to Modify:** `/src/PRFactory.Infrastructure/Application/PlanReviewService.cs`

In `AddCommentAsync()` method, add:

```csharp
// After creating comment:
if (mentionedUserIds != null && mentionedUserIds.Any())
{
    var author = await _userRepo.GetByIdAsync(authorId);
    await _notificationService.NotifyMentionedInCommentAsync(
        mentionedUserIds,
        ticketId,
        comment.Id,
        author?.DisplayName ?? "Someone");
}
```

---

## Summary

**Total Estimated Lines of Code:**
- Entity: ~75 lines
- Repository Interface: ~20 lines
- Repository Implementation: ~110 lines
- Service Interface: ~25 lines
- Service Implementation: ~190 lines
- DTO: ~40 lines
- NotificationBell Component: ~195 lines
- Integration code: ~20 lines
- Migration: ~30 lines

**Total: ~705 lines of code**

**Estimated Effort:** 3-4 days
- Day 1: Database schema, entity, repository
- Day 2: Service implementation
- Day 3: Blazor component and integration
- Day 4: Testing and refinement

---

## Testing

### Unit Tests

- `NotificationService.NotifyReviewerAssigned_CreatesNotification`
- `NotificationService.NotifyMentioned_CreatesMultipleNotifications`
- `NotificationRepository.GetUnreadCount_ReturnsCorrectCount`
- `Notification.MarkAsRead_SetsReadAtTimestamp`

### Integration Tests

- Assign reviewer → notification created
- Add comment with @mention → notification created
- Mark notification as read → IsRead = true
- Mark all as read → all unread become read

---

## Future Enhancements

1. **Email Notifications**
   - Send email for critical notifications
   - User preferences for email frequency

2. **Real-time Updates (SignalR)**
   - Push notifications to connected clients
   - No polling needed

3. **Notification Preferences**
   - User can choose which notifications to receive
   - Mute specific tickets

4. **Slack/Teams Integration**
   - Post notifications to team channels
   - DM users on Slack/Teams
