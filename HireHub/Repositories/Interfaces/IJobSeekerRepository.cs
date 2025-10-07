using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IJobSeekerRepository
    {
     
        Task<IEnumerable<JobSeeker>> GetAllAsync();
        Task<JobSeeker?> GetByIdAsync(Guid id);


        Task<JobSeeker?> GetByUserIdAsync(Guid userId);

      
        Task<IEnumerable<JobSeeker>> SearchByCollegeAsync(string college);

        
        Task<IEnumerable<JobSeeker>> SearchBySkillAsync(string skill);

        Task<JobSeeker> AddAsync(JobSeeker jobSeeker);
        Task<JobSeeker> UpdateAsync(JobSeeker jobSeeker);
        Task<bool> DeleteAsync(Guid id);
        
        Task<bool> HasDependentsAsync(Guid jobSeekerId);


    
        Task<bool> ExistsForUserAsync(Guid userId);
    }
}
