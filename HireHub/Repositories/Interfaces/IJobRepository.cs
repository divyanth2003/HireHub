using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IJobRepository
    {
       
        Task<IEnumerable<Job>> GetAllAsync();
        Task<Job?> GetByIdAsync(int id);
        Task<IEnumerable<Job>> GetByEmployerAsync(Guid employerId);

   
        Task<IEnumerable<Job>> SearchByTitleAsync(string title);
        Task<IEnumerable<Job>> SearchByLocationAsync(string location);
        Task<IEnumerable<Job>> SearchBySkillAsync(string skill);

       
        Task<IEnumerable<Job>> SearchByCompanyAsync(string company);



        
        Task<Job> AddAsync(Job job);
        Task<Job> UpdateAsync(Job job);
        Task<bool> DeleteAsync(int id);
    }
}
