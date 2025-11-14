using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Application;
using Xunit;

namespace PRFactory.Infrastructure.Tests.Application;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepoMock;
    private readonly Mock<ITicketRepository> _ticketRepoMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _notificationRepoMock = new Mock<INotificationRepository>();
        _ticketRepoMock = new Mock<ITicketRepository>();
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _service = new NotificationService(_notificationRepoMock.Object, _ticketRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task NotifyReviewerAssigned_CreatesNotification()
    {
        // Arrange
        var reviewerId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        ticket.UpdateTicketInfo("Test Ticket", "Description");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);

        Notification? capturedNotification = null;
        _notificationRepoMock.Setup(x => x.CreateAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => capturedNotification = n)
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyReviewerAssignedAsync(reviewerId, ticketId, false);

        // Assert
        _notificationRepoMock.Verify(x => x.CreateAsync(It.IsAny<Notification>()), Times.Once);
        Assert.NotNull(capturedNotification);
        Assert.Equal(reviewerId, capturedNotification.UserId);
        Assert.Equal(ticketId, capturedNotification.TicketId);
        Assert.Equal(NotificationType.ReviewerAssigned, capturedNotification.Type);
        Assert.Contains("optional", capturedNotification.Title);
    }

    [Fact]
    public async Task NotifyReviewerAssigned_RequiredReviewer_IncludesRequiredInMessage()
    {
        // Arrange
        var reviewerId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        ticket.UpdateTicketInfo("Test Ticket", "Description");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);

        Notification? capturedNotification = null;
        _notificationRepoMock.Setup(x => x.CreateAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => capturedNotification = n)
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyReviewerAssignedAsync(reviewerId, ticketId, true);

        // Assert
        Assert.NotNull(capturedNotification);
        Assert.Contains("required", capturedNotification.Title);
        Assert.Contains("required", capturedNotification.Message);
    }

    [Fact]
    public async Task NotifyMentionedInComment_CreatesMultipleNotifications()
    {
        // Arrange
        var mentionedUserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var ticketId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var commentAuthor = "John Doe";
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        ticket.UpdateTicketInfo("Test Ticket", "Description");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);

        List<Notification>? capturedNotifications = null;
        _notificationRepoMock.Setup(x => x.CreateManyAsync(It.IsAny<List<Notification>>()))
            .Callback<List<Notification>>(n => capturedNotifications = n)
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyMentionedInCommentAsync(mentionedUserIds, ticketId, commentId, commentAuthor);

        // Assert
        _notificationRepoMock.Verify(x => x.CreateManyAsync(It.IsAny<List<Notification>>()), Times.Once);
        Assert.NotNull(capturedNotifications);
        Assert.Equal(3, capturedNotifications.Count);
        Assert.All(capturedNotifications, n =>
        {
            Assert.Equal(NotificationType.MentionedInComment, n.Type);
            Assert.Equal(ticketId, n.TicketId);
            Assert.Equal(commentId, n.RelatedEntityId);
            Assert.Contains(commentAuthor, n.Title);
        });
    }

    [Fact]
    public async Task NotifyPlanApproved_CreatesNotificationForAllReviewers()
    {
        // Arrange
        var reviewerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        ticket.UpdateTicketInfo("Test Ticket", "Description");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);

        List<Notification>? capturedNotifications = null;
        _notificationRepoMock.Setup(x => x.CreateManyAsync(It.IsAny<List<Notification>>()))
            .Callback<List<Notification>>(n => capturedNotifications = n)
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyPlanApprovedAsync(ticketId, reviewerIds);

        // Assert
        _notificationRepoMock.Verify(x => x.CreateManyAsync(It.IsAny<List<Notification>>()), Times.Once);
        Assert.NotNull(capturedNotifications);
        Assert.Equal(2, capturedNotifications.Count);
        Assert.All(capturedNotifications, n =>
        {
            Assert.Equal(NotificationType.PlanApproved, n.Type);
            Assert.Equal(ticketId, n.TicketId);
            Assert.Contains("approved", n.Title);
        });
    }

    [Fact]
    public async Task NotifyPlanRejected_IncludesReasonInMessage()
    {
        // Arrange
        var reviewerIds = new List<Guid> { Guid.NewGuid() };
        var ticketId = Guid.NewGuid();
        var reason = "Code quality issues";
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        ticket.UpdateTicketInfo("Test Ticket", "Description");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);

        List<Notification>? capturedNotifications = null;
        _notificationRepoMock.Setup(x => x.CreateManyAsync(It.IsAny<List<Notification>>()))
            .Callback<List<Notification>>(n => capturedNotifications = n)
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyPlanRejectedAsync(ticketId, reviewerIds, reason);

        // Assert
        Assert.NotNull(capturedNotifications);
        Assert.Single(capturedNotifications);
        Assert.Equal(NotificationType.PlanRejected, capturedNotifications[0].Type);
        Assert.Contains(reason, capturedNotifications[0].Message);
        Assert.Contains("rejected", capturedNotifications[0].Title);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCount = 5;

        _notificationRepoMock.Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _service.GetUnreadCountAsync(userId);

        // Assert
        Assert.Equal(expectedCount, result);
        _notificationRepoMock.Verify(x => x.GetUnreadCountAsync(userId), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_SetsReadAtTimestamp()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.ReviewerAssigned,
            Guid.NewGuid(),
            "Title",
            "Message");

        _notificationRepoMock.Setup(x => x.GetByIdAsync(notificationId))
            .ReturnsAsync(notification);

        _notificationRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Notification>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.MarkAsReadAsync(notificationId);

        // Assert
        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAt);
        _notificationRepoMock.Verify(x => x.UpdateAsync(notification), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_MarksAllUnreadNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _notificationRepoMock.Setup(x => x.MarkAllAsReadAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.MarkAllAsReadAsync(userId);

        // Assert
        _notificationRepoMock.Verify(x => x.MarkAllAsReadAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetRecentNotifications_ReturnsLimitedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var limit = 5;
        var notifications = new List<Notification>
        {
            Notification.Create(userId, NotificationType.ReviewerAssigned, Guid.NewGuid(), "Title1", "Message1"),
            Notification.Create(userId, NotificationType.MentionedInComment, Guid.NewGuid(), "Title2", "Message2"),
            Notification.Create(userId, NotificationType.PlanApproved, Guid.NewGuid(), "Title3", "Message3")
        };

        _notificationRepoMock.Setup(x => x.GetRecentAsync(userId, limit))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetRecentNotificationsAsync(userId, limit);

        // Assert
        Assert.Equal(3, result.Count);
        _notificationRepoMock.Verify(x => x.GetRecentAsync(userId, limit), Times.Once);
    }

    [Fact]
    public async Task NotifyMentioned_WithInvalidTicket_LogsWarning()
    {
        // Arrange
        var mentionedUserIds = new List<Guid> { Guid.NewGuid() };
        var ticketId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var commentAuthor = "John Doe";

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync((Ticket?)null);

        // Act
        await _service.NotifyMentionedInCommentAsync(mentionedUserIds, ticketId, commentId, commentAuthor);

        // Assert
        _notificationRepoMock.Verify(x => x.CreateManyAsync(It.IsAny<List<Notification>>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_AlreadyRead_DoesNothing()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.ReviewerAssigned,
            Guid.NewGuid(),
            "Title",
            "Message");

        // Mark as read first
        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;

        _notificationRepoMock.Setup(x => x.GetByIdAsync(notificationId))
            .ReturnsAsync(notification);

        _notificationRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Notification>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.MarkAsReadAsync(notificationId);

        // Assert
        Assert.True(notification.IsRead);
        Assert.Equal(firstReadAt, notification.ReadAt); // ReadAt should not change
        _notificationRepoMock.Verify(x => x.UpdateAsync(notification), Times.Once);
    }

    [Fact]
    public void Notification_MarkAsUnread_ClearsReadAt()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.ReviewerAssigned,
            Guid.NewGuid(),
            "Title",
            "Message");

        // Mark as read first
        notification.MarkAsRead();
        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAt);

        // Act
        notification.MarkAsUnread();

        // Assert
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task NotifyCommentReply_CreatesNotification()
    {
        // Arrange
        var originalAuthorId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var replyAuthor = "Jane Doe";
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        ticket.UpdateTicketInfo("Test Ticket", "Description");

        _ticketRepoMock.Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticket);

        Notification? capturedNotification = null;
        _notificationRepoMock.Setup(x => x.CreateAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => capturedNotification = n)
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyCommentReplyAsync(originalAuthorId, ticketId, commentId, replyAuthor);

        // Assert
        _notificationRepoMock.Verify(x => x.CreateAsync(It.IsAny<Notification>()), Times.Once);
        Assert.NotNull(capturedNotification);
        Assert.Equal(originalAuthorId, capturedNotification.UserId);
        Assert.Equal(ticketId, capturedNotification.TicketId);
        Assert.Equal(commentId, capturedNotification.RelatedEntityId);
        Assert.Equal(NotificationType.CommentReply, capturedNotification.Type);
        Assert.Contains(replyAuthor, capturedNotification.Title);
    }

    [Fact]
    public async Task GetNotificationsAsync_ReturnsMappedDtos()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create("TEST-123", Guid.NewGuid(), Guid.NewGuid());
        ticket.UpdateTicketInfo("Test Ticket", "Description");

        var notifications = new List<Notification>
        {
            Notification.Create(userId, NotificationType.ReviewerAssigned, ticketId, "Title1", "Message1"),
            Notification.Create(userId, NotificationType.PlanApproved, ticketId, "Title2", "Message2")
        };

        // Set up ticket navigation property (simulating EF Core Include)
        foreach (var notification in notifications)
        {
            typeof(Notification).GetProperty("Ticket")!.SetValue(notification, ticket);
        }

        _notificationRepoMock.Setup(x => x.GetByUserIdAsync(userId, 50))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetNotificationsAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, dto =>
        {
            Assert.Equal(userId, dto.UserId);
            Assert.Equal(ticketId, dto.TicketId);
            Assert.Equal("Test Ticket", dto.TicketTitle);
        });
    }

    [Fact]
    public async Task GetUnreadNotificationsAsync_ReturnsOnlyUnread()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var unreadNotifications = new List<Notification>
        {
            Notification.Create(userId, NotificationType.ReviewerAssigned, Guid.NewGuid(), "Title1", "Message1"),
            Notification.Create(userId, NotificationType.MentionedInComment, Guid.NewGuid(), "Title2", "Message2")
        };

        _notificationRepoMock.Setup(x => x.GetUnreadByUserIdAsync(userId))
            .ReturnsAsync(unreadNotifications);

        // Act
        var result = await _service.GetUnreadNotificationsAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal(userId, dto.UserId));
    }
}
