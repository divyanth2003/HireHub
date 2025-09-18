using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IApplicationRepository
    {
        // ------------------- GET -------------------
        Task<IEnumerable<Application>> GetAllAsync();
        Task<Application?> GetByIdAsync(int id);
        Task<IEnumerable<Application>> GetByJobAsync(int jobId);
        Task<IEnumerable<Application>> GetByJobSeekerAsync(Guid jobSeekerId);
        Task<Application?> GetByIdWithDetailsAsync(int id);


        // ------------------- ADD/UPDATE/DELETE -------------------
        Task<Application> AddAsync(Application application);
        Task<Application> UpdateAsync(Application application);
        Task<bool> DeleteAsync(int id);

        // ------------------- UTILITIES -------------------
        // Get shortlisted applications for a job
        Task<IEnumerable<Application>> GetShortlistedByJobAsync(int jobId);

        // Get applications with scheduled interviews
        Task<IEnumerable<Application>> GetWithInterviewAsync(int jobId);

        // Mark application as reviewed
        Task<Application?> MarkReviewedAsync(int applicationId, string? notes = null);
    }
}
