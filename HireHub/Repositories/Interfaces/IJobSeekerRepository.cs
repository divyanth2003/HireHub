using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IJobSeekerRepository
    {
        // ------------------- GET -------------------
        Task<IEnumerable<JobSeeker>> GetAllAsync();
        Task<JobSeeker?> GetByIdAsync(Guid id);

        // New: find by UserId (since JobSeeker always linked to User)
        Task<JobSeeker?> GetByUserIdAsync(Guid userId);

        // New: search by College
        Task<IEnumerable<JobSeeker>> SearchByCollegeAsync(string college);

        // New: search by Skill keyword
        Task<IEnumerable<JobSeeker>> SearchBySkillAsync(string skill);

        // ------------------- ADD/UPDATE/DELETE -------------------
        Task<JobSeeker> AddAsync(JobSeeker jobSeeker);
        Task<JobSeeker> UpdateAsync(JobSeeker jobSeeker);
        Task<bool> DeleteAsync(Guid id);
        // Utility: check if a JobSeeker has any dependent data (Resumes / Applications)
        Task<bool> HasDependentsAsync(Guid jobSeekerId);


        // Utility: check if a JobSeeker exists for a given UserId
        Task<bool> ExistsForUserAsync(Guid userId);
    }
}
