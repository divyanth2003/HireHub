using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        // Get notifications for a user (all)
        Task<IEnumerable<Notification>> GetByUserAsync(Guid userId);

        // Get unread notifications for a user
        Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid userId);

        // Get recent notifications for a user (limit)
        Task<IEnumerable<Notification>> GetRecentByUserAsync(Guid userId, int limit = 20);

        // Get a single notification
        Task<Notification?> GetByIdAsync(int id);

        // Add / Update / Delete
        Task<Notification> AddAsync(Notification notification);
        Task<Notification> UpdateAsync(Notification notification);
        Task<bool> DeleteAsync(int id);

        // Utilities
        Task<bool> MarkAsReadAsync(int id);
        Task<int> MarkAllAsReadAsync(Guid userId); // returns count updated
        Task<IEnumerable<Notification>> GetUnsentEmailNotificationsAsync(int limit = 50);
        Task<bool> SetSentEmailAsync(int notificationId);
    }
}
