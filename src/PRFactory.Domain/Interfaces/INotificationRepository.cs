using PRFactory.Domain.Entities;

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
