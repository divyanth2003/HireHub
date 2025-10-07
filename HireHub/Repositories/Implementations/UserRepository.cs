using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.Data;
using Microsoft.EntityFrameworkCore;

namespace HireHub.API.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly HireHubContext _context;

        public UserRepository(HireHubContext context)
        {
            _context = context;
        }

    
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Employer)
                .Include(u => u.JobSeeker)
                .Include(u => u.Notifications)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Employer)
                .Include(u => u.JobSeeker)
                .Include(u => u.Notifications)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.Employer)
                .Include(u => u.JobSeeker)
                .Include(u => u.Notifications)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(string role)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .Include(u => u.Employer)
                .Include(u => u.JobSeeker)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> SearchByNameAsync(string name)
        {
            return await _context.Users
                .Where(u => u.FullName.Contains(name))
                .Include(u => u.Employer)
                .Include(u => u.JobSeeker)
                .ToListAsync();
        }

        public async Task<User> AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}
