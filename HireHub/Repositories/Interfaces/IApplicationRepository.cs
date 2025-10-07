using HireHub.API.Models;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IApplicationRepository
    {
    
        Task<IEnumerable<Application>> GetAllAsync();
        Task<Application?> GetByIdAsync(int id);
        Task<IEnumerable<Application>> GetByJobAsync(int jobId);
        Task<IEnumerable<Application>> GetByJobSeekerAsync(Guid jobSeekerId);
        Task<Application?> GetByIdWithDetailsAsync(int id);


        Task<Application> AddAsync(Application application);
        Task<Application> UpdateAsync(Application application);
        Task<bool> DeleteAsync(int id);

        
        Task<IEnumerable<Application>> GetShortlistedByJobAsync(int jobId);

        
        Task<IEnumerable<Application>> GetWithInterviewAsync(int jobId);

       
        Task<Application?> MarkReviewedAsync(int applicationId, string? notes = null);
    }
}
