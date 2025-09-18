using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.Data;
using Microsoft.EntityFrameworkCore;

namespace HireHub.API.Repositories.Implementations
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly HireHubContext _context;

        public ApplicationRepository(HireHubContext context)
        {
            _context = context;
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<Application>> GetAllAsync()
        {
            return await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.JobSeeker).ThenInclude(js => js.User)
                .Include(a => a.Resume)
                .ToListAsync();
        }

        public async Task<Application?> GetByIdAsync(int id)
        {
            return await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.JobSeeker).ThenInclude(js => js.User)
                .Include(a => a.Resume)
                .FirstOrDefaultAsync(a => a.ApplicationId == id);
        }

        public async Task<IEnumerable<Application>> GetByJobAsync(int jobId)
        {
            return await _context.Applications
                .Where(a => a.JobId == jobId)
                .Include(a => a.Job)
                .Include(a => a.JobSeeker).ThenInclude(js => js.User)
                .Include(a => a.Resume)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetByJobSeekerAsync(Guid jobSeekerId)
        {
            return await _context.Applications
                .Where(a => a.JobSeekerId == jobSeekerId)
                .Include(a => a.Job)
                .Include(a => a.Resume)
                .ToListAsync();
        }

        public async Task<Application?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Applications
                .Where(a => a.ApplicationId == id)
                .Include(a => a.Job).ThenInclude(j => j.Employer).ThenInclude(e => e.User)
                .Include(a => a.JobSeeker).ThenInclude(js => js.User)
                .Include(a => a.Resume)
                .FirstOrDefaultAsync();
        }


        // ------------------- ADD/UPDATE/DELETE -------------------
        public async Task<Application> AddAsync(Application application)
        {
            _context.Applications.Add(application);
            await _context.SaveChangesAsync();
            return application;
        }

        public async Task<Application> UpdateAsync(Application application)
        {
            _context.Applications.Update(application);
            await _context.SaveChangesAsync();
            return application;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var app = await _context.Applications.FindAsync(id);
            if (app == null) return false;

            _context.Applications.Remove(app);
            await _context.SaveChangesAsync();
            return true;
        }

        // ------------------- UTILITIES -------------------
        public async Task<IEnumerable<Application>> GetShortlistedByJobAsync(int jobId)
        {
            return await _context.Applications
                .Where(a => a.JobId == jobId && a.IsShortlisted)
                .Include(a => a.Job)
                .Include(a => a.JobSeeker).ThenInclude(js => js.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetWithInterviewAsync(int jobId)
        {
            return await _context.Applications
                .Where(a => a.JobId == jobId && a.InterviewDate != null)
                .Include(a => a.Job)
                .Include(a => a.JobSeeker).ThenInclude(js => js.User)
                .ToListAsync();
        }

        public async Task<Application?> MarkReviewedAsync(int applicationId, string? notes = null)
        {
            var app = await _context.Applications.FindAsync(applicationId);
            if (app == null) return null;

            app.ReviewedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(notes))
                app.Notes = notes;

            _context.Applications.Update(app);
            await _context.SaveChangesAsync();

            return app;
        }
    }
}
