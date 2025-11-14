using CoreNotificationDto = PRFactory.Core.Application.DTOs.NotificationDto;

namespace PRFactory.Web.Models;

/// <summary>
/// Web layer DTO for Notification.
/// This is an alias to the Core layer DTO for consistency with Web layer conventions.
/// </summary>
public class NotificationDto : CoreNotificationDto
{
}
