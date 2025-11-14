using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using System.Timers;

namespace PRFactory.Web.Components.Notifications;

public partial class NotificationBell : IDisposable
{
    [Inject]
    private INotificationService NotificationService { get; set; } = null!;

    [Inject]
    private ICurrentUserService CurrentUserService { get; set; } = null!;

    private List<PRFactory.Core.Application.DTOs.NotificationDto> notifications = new();
    private int unreadCount = 0;
    private System.Timers.Timer? pollTimer;
    private string? errorMessage;

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

        try
        {
            notifications = await NotificationService.GetRecentNotificationsAsync(currentUser.Id, 10);
            unreadCount = await NotificationService.GetUnreadCountAsync(currentUser.Id);
            errorMessage = null;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load notifications: {ex.Message}";
        }

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

    public void Dispose()
    {
        pollTimer?.Stop();
        pollTimer?.Dispose();
    }
}
