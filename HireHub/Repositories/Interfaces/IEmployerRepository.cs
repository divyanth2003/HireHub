using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IEmployerRepository
    {
      
        Task<IEnumerable<Employer>> GetAllAsync();
        Task<Employer?> GetByIdAsync(Guid id);

        Task<Employer?> GetByUserIdAsync(Guid userId);

       
        Task<Employer?> GetByJobIdAsync(int jobId);


  
        Task<IEnumerable<Employer>> SearchByCompanyNameAsync(string companyName);

        Task<Employer> AddAsync(Employer employer);
        Task<Employer> UpdateAsync(Employer employer);
        Task<bool> DeleteAsync(Guid id);

       
        Task<bool> ExistsByCompanyNameAsync(string companyName);
    }
}
