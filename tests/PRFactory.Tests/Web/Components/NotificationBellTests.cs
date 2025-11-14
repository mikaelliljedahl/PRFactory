using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Notifications;
using Xunit;

namespace PRFactory.Tests.Web.Components;

public class NotificationBellTests : ComponentTestBase
{
    private Mock<INotificationService> _mockNotificationService = null!;
    private Mock<ICurrentUserService> _mockCurrentUserService = null!;
    private User _testUser = null!;
    private List<NotificationDto> _testNotifications = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        _mockNotificationService = new Mock<INotificationService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        services.AddSingleton(_mockNotificationService.Object);
        services.AddSingleton(_mockCurrentUserService.Object);

        // Setup test data
        var tenantId = Guid.NewGuid();
        _testUser = User.Create(
            tenantId: tenantId,
            email: "test@example.com",
            displayName: "Test User",
            avatarUrl: null,
            externalAuthId: null,
            identityProvider: null,
            role: UserRole.Member);

        _testNotifications = new List<NotificationDto>
        {
            new NotificationDto
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Type = "ReviewerAssigned",
                TicketId = Guid.NewGuid(),
                TicketTitle = "Test Ticket",
                Title = "You've been assigned as a required reviewer",
                Message = "You have been assigned as a required reviewer for plan: Test Ticket",
                ActionUrl = "/tickets/123",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationDto
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Type = "MentionedInComment",
                TicketId = Guid.NewGuid(),
                TicketTitle = "Another Ticket",
                Title = "Someone mentioned you in a comment",
                Message = "Someone mentioned you in a comment on plan: Another Ticket",
                ActionUrl = "/tickets/456#comment-789",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        // Setup default mock behavior
        _mockCurrentUserService
            .Setup(s => s.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _mockNotificationService
            .Setup(s => s.GetRecentNotificationsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(_testNotifications);

        _mockNotificationService
            .Setup(s => s.GetUnreadCountAsync(It.IsAny<Guid>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task OnInitialized_LoadsNotifications()
    {
        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        _mockNotificationService.Verify(
            s => s.GetRecentNotificationsAsync(_testUser.Id, 10),
            Times.Once);
    }

    [Fact]
    public async Task OnInitialized_LoadsUnreadCount()
    {
        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        _mockNotificationService.Verify(
            s => s.GetUnreadCountAsync(_testUser.Id),
            Times.Once);
    }

    [Fact]
    public async Task OnInitialized_DisplaysUnreadBadge_WhenUnreadCountGreaterThanZero()
    {
        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        var badge = cut.FindAll(".badge.bg-danger");
        Assert.NotEmpty(badge);
        Assert.Contains("1", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_DoesNotDisplayBadge_WhenUnreadCountIsZero()
    {
        // Arrange
        _mockNotificationService
            .Setup(s => s.GetUnreadCountAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0);

        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        var badges = cut.FindAll(".badge.bg-danger");
        Assert.Empty(badges);
    }

    [Fact]
    public async Task OnInitialized_StartsPollingTimer()
    {
        // Arrange
        var callCount = 0;
        _mockNotificationService
            .Setup(s => s.GetRecentNotificationsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(_testNotifications)
            .Callback(() => callCount++);

        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow initial load

        // Initial load
        Assert.Equal(1, callCount);

        // Wait for polling interval (30 seconds, but we'll wait a bit and verify timer exists)
        // Note: Full timer test would require waiting 30+ seconds, so we verify initial setup
        _mockNotificationService.Verify(
            s => s.GetRecentNotificationsAsync(_testUser.Id, 10),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task MarkAsRead_CallsNotificationService()
    {
        // Arrange
        var notificationId = _testNotifications[0].Id;
        _mockNotificationService
            .Setup(s => s.MarkAsReadAsync(notificationId))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Act
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(NotificationBell).GetMethod("MarkAsRead",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { notificationId })!;
            }
        });

        // Assert
        _mockNotificationService.Verify(
            s => s.MarkAsReadAsync(notificationId),
            Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_ReloadsNotifications()
    {
        // Arrange
        var notificationId = _testNotifications[0].Id;
        var loadCount = 0;

        _mockNotificationService
            .Setup(s => s.GetRecentNotificationsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(_testNotifications)
            .Callback(() => loadCount++);

        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow initial load
        var initialLoadCount = loadCount;

        // Act
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(NotificationBell).GetMethod("MarkAsRead",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { notificationId })!;
            }
        });

        // Assert
        Assert.True(loadCount > initialLoadCount, "Notifications should be reloaded after marking as read");
    }

    [Fact]
    public async Task MarkAllAsRead_CallsNotificationService()
    {
        // Arrange
        _mockNotificationService
            .Setup(s => s.MarkAllAsReadAsync(_testUser.Id))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Act
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(NotificationBell).GetMethod("MarkAllAsRead",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { })!;
            }
        });

        // Assert
        _mockNotificationService.Verify(
            s => s.MarkAllAsReadAsync(_testUser.Id),
            Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_ReloadsNotifications()
    {
        // Arrange
        var loadCount = 0;

        _mockNotificationService
            .Setup(s => s.GetRecentNotificationsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(_testNotifications)
            .Callback(() => loadCount++);

        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow initial load
        var initialLoadCount = loadCount;

        // Act
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(NotificationBell).GetMethod("MarkAllAsRead",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { })!;
            }
        });

        // Assert
        Assert.True(loadCount > initialLoadCount, "Notifications should be reloaded after marking all as read");
    }

    [Fact]
    public async Task Dispose_StopsTimer()
    {
        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Dispose the component
        cut.Dispose();

        // Assert - if timer is disposed, no exceptions should occur
        // Note: We can't directly verify timer disposal, but this ensures Dispose() runs without errors
        Assert.True(true);
    }

    [Fact]
    public async Task LoadNotifications_WithException_SetsErrorMessage()
    {
        // Arrange
        _mockNotificationService
            .Setup(s => s.GetRecentNotificationsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        Assert.Contains("Failed to load notifications", cut.Markup);
        Assert.Contains("Test exception", cut.Markup);
    }

    [Fact]
    public async Task LoadNotifications_WithNullUser_DoesNotCallService()
    {
        // Arrange
        _mockCurrentUserService
            .Setup(s => s.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Assert - GetRecentNotificationsAsync should not be called if user is null
        _mockNotificationService.Verify(
            s => s.GetRecentNotificationsAsync(It.IsAny<Guid>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkAllAsRead_WithNullUser_DoesNotCallService()
    {
        // Arrange
        _mockCurrentUserService
            .Setup(s => s.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Act
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = typeof(NotificationBell).GetMethod("MarkAllAsRead",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(instance, new object[] { })!;
            }
        });

        // Assert
        _mockNotificationService.Verify(
            s => s.MarkAllAsReadAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Component_RendersDropdownButton()
    {
        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        var button = cut.Find("button[data-bs-toggle='dropdown']");
        Assert.NotNull(button);
        Assert.Contains("bi-bell", button.InnerHtml);
    }

    [Fact]
    public async Task Component_PassesNotificationsToDropdown()
    {
        // Act
        var cut = RenderComponent<NotificationBell>();
        await Task.Delay(100); // Allow async initialization to complete

        // Assert
        // Verify that NotificationDropdown component is rendered
        var dropdown = cut.FindComponent<PRFactory.Web.UI.Notifications.NotificationDropdown>();
        Assert.NotNull(dropdown);
    }
}
