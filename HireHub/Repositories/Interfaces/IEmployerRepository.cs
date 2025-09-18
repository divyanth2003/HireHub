using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IEmployerRepository
    {
        // ------------------- GET -------------------
        Task<IEnumerable<Employer>> GetAllAsync();
        Task<Employer?> GetByIdAsync(Guid id);

        // New: find employer by UserId (since Employer always linked to a User)
        Task<Employer?> GetByUserIdAsync(Guid userId);

        // Get employer by jobId (fetches Employer who posted the job)
        Task<Employer?> GetByJobIdAsync(int jobId);


        // New: search employers by company name (useful for jobseeker search)
        Task<IEnumerable<Employer>> SearchByCompanyNameAsync(string companyName);

        // ------------------- ADD/UPDATE/DELETE -------------------
        Task<Employer> AddAsync(Employer employer);
        Task<Employer> UpdateAsync(Employer employer);
        Task<bool> DeleteAsync(Guid id);

        // Utility: check if company already exists
        Task<bool> ExistsByCompanyNameAsync(string companyName);
    }
}
