using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IJobRepository
    {
        // ------------------- GET -------------------
        Task<IEnumerable<Job>> GetAllAsync();
        Task<Job?> GetByIdAsync(int id);
        Task<IEnumerable<Job>> GetByEmployerAsync(Guid employerId);

        // New: Search / filter methods
        Task<IEnumerable<Job>> SearchByTitleAsync(string title);
        Task<IEnumerable<Job>> SearchByLocationAsync(string location);
        Task<IEnumerable<Job>> SearchBySkillAsync(string skill);

        // in IJobRepository
        // IJobRepository.cs
        Task<IEnumerable<Job>> SearchByCompanyAsync(string company);



        // ------------------- ADD/UPDATE/DELETE -------------------
        Task<Job> AddAsync(Job job);
        Task<Job> UpdateAsync(Job job);
        Task<bool> DeleteAsync(int id);
    }
}
