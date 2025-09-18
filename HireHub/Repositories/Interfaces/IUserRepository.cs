using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IUserRepository
    {
        // ------------------- GET -------------------
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);

        // Search/filter (new)
        Task<IEnumerable<User>> GetByRoleAsync(string role);
        Task<IEnumerable<User>> SearchByNameAsync(string name);

        // ------------------- ADD/UPDATE/DELETE -------------------
        Task<User> AddAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(Guid id);

        // Existence checks (new)
        Task<bool> ExistsByEmailAsync(string email);
    }
}
