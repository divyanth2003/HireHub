using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IResumeRepository
    {
        // ------------------- GET -------------------
        Task<IEnumerable<Resume>> GetAllAsync();
        Task<Resume?> GetByIdAsync(int id);
        Task<IEnumerable<Resume>> GetByJobSeekerAsync(Guid jobSeekerId);

        // New: Get default resume for a jobseeker
        Task<Resume?> GetDefaultByJobSeekerAsync(Guid jobSeekerId);

        // ------------------- ADD/UPDATE/DELETE -------------------
        Task<Resume> AddAsync(Resume resume);
        Task<Resume> UpdateAsync(Resume resume);
        Task<bool> DeleteAsync(int id);

        // ------------------- UTILITIES -------------------
        // Set a given resume as default (unset others)
        Task SetDefaultAsync(Guid jobSeekerId, int resumeId);

        // Check if jobseeker already has a resume with same name
        Task<bool> ExistsByNameAsync(Guid jobSeekerId, string resumeName);
    }
}
