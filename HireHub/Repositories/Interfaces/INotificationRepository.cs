using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetByUserAsync(Guid userId);

       
        Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid userId);

        
        Task<IEnumerable<Notification>> GetRecentByUserAsync(Guid userId, int limit = 20);

   
        Task<Notification?> GetByIdAsync(int id);


        
        Task<Notification> AddAsync(Notification notification);
        Task<Notification> UpdateAsync(Notification notification);
        Task<bool> DeleteAsync(int id);

   
        Task<bool> MarkAsReadAsync(int id);
        Task<int> MarkAllAsReadAsync(Guid userId); // returns count updated
        Task<IEnumerable<Notification>> GetUnsentEmailNotificationsAsync(int limit = 50);
        Task<bool> SetSentEmailAsync(int notificationId);
    }
}
