using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IUserRepository
    {
    
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);

      
        Task<IEnumerable<User>> GetByRoleAsync(string role);
        Task<IEnumerable<User>> SearchByNameAsync(string name);

        Task<User> AddAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(Guid id);

       
        Task<bool> ExistsByEmailAsync(string email);
    }
}
