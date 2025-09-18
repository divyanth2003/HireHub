using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.Data;
using Microsoft.EntityFrameworkCore;

namespace HireHub.API.Repositories.Implementations
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly HireHubContext _context;

        public NotificationRepository(HireHubContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notification>> GetByUserAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .Include(n => n.User)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .Include(n => n.User)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetRecentByUserAsync(Guid userId, int limit = 20)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .Include(n => n.User)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.NotificationId == id);
        }

        

        public async Task<Notification> AddAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return await _context.Notifications
                .Include(n => n.User)
                .FirstAsync(n => n.NotificationId == notification.NotificationId);
        }

        public async Task<Notification> UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return await _context.Notifications
                .Include(n => n.User)
                .FirstAsync(n => n.NotificationId == notification.NotificationId);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var notif = await _context.Notifications.FindAsync(id);
            if (notif == null) return false;

            _context.Notifications.Remove(notif);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            var n = await _context.Notifications.FindAsync(id);
            if (n == null) return false;
            if (!n.IsRead)
            {
                n.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            var items = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!items.Any()) return 0;

            foreach (var n in items) n.IsRead = true;
            await _context.SaveChangesAsync();
            return items.Count;
        }

        // Notifications that haven't been sent via email yet (SentEmail == false)
        public async Task<IEnumerable<Notification>> GetUnsentEmailNotificationsAsync(int limit = 50)
        {
            return await _context.Notifications
                .Where(n => !n.SentEmail)
                .Include(n => n.User)
                .OrderBy(n => n.CreatedAt) // oldest first to send older ones first
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> SetSentEmailAsync(int notificationId)
        {
            var n = await _context.Notifications.FindAsync(notificationId);
            if (n == null) return false;
            n.SentEmail = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
