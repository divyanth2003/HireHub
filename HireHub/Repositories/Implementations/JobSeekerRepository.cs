using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.Data;
using Microsoft.EntityFrameworkCore;

namespace HireHub.API.Repositories.Implementations
{
    public class JobSeekerRepository : IJobSeekerRepository
    {
        private readonly HireHubContext _context;

        public JobSeekerRepository(HireHubContext context)
        {
            _context = context;
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<JobSeeker>> GetAllAsync()
        {
            return await _context.JobSeekers
                .Include(js => js.User)
                .Include(js => js.Resumes)
                .Include(js => js.Applications)
                .ToListAsync();
        }

        public async Task<JobSeeker?> GetByIdAsync(Guid id)
        {
            return await _context.JobSeekers
                .Include(js => js.User)
                .Include(js => js.Resumes)
                .Include(js => js.Applications)
                .FirstOrDefaultAsync(js => js.JobSeekerId == id);
        }

        public async Task<JobSeeker?> GetByUserIdAsync(Guid userId)
        {
            return await _context.JobSeekers
                .Include(js => js.User)
                .Include(js => js.Resumes)
                .Include(js => js.Applications)
                .FirstOrDefaultAsync(js => js.UserId == userId);
        }

        public async Task<IEnumerable<JobSeeker>> SearchByCollegeAsync(string college)
        {
            return await _context.JobSeekers
                .Where(js => js.College != null && js.College.Contains(college))
                .Include(js => js.User)
                .Include(js => js.Resumes)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobSeeker>> SearchBySkillAsync(string skill)
        {
            return await _context.JobSeekers
                .Where(js => js.Skills != null && js.Skills.Contains(skill))
                .Include(js => js.User)
                .Include(js => js.Resumes)
                .ToListAsync();
        }

        // ------------------- ADD/UPDATE/DELETE -------------------
        public async Task<JobSeeker> AddAsync(JobSeeker jobSeeker)
        {
            _context.JobSeekers.Add(jobSeeker);
            await _context.SaveChangesAsync();
            return jobSeeker;
        }

        public async Task<JobSeeker> UpdateAsync(JobSeeker jobSeeker)
        {
            _context.JobSeekers.Update(jobSeeker);
            await _context.SaveChangesAsync();
            return jobSeeker;
        }

        // JobSeekerRepository.cs (or the repository file you already use)
        public async Task<bool> DeleteAsync(Guid id)
        {
            // find entity
            var jobSeeker = await _context.JobSeekers.FindAsync(id);
            if (jobSeeker == null) return false;

            // Check dependent tables - adjust DbSet names to match your context
            var hasResumes = await _context.Resumes.AnyAsync(r => r.JobSeekerId == id);
            if (hasResumes) return false;

            var hasApplications = await _context.Applications.AnyAsync(a => a.JobSeekerId == id);
            if (hasApplications) return false;

            // add other checks if you have more related entities:
            // var hasOther = await _context.SomeOtherEntity.AnyAsync(x => x.JobSeekerId == id);
            // if (hasOther) return false;

            // safe to remove
            _context.JobSeekers.Remove(jobSeeker);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> HasDependentsAsync(Guid jobSeekerId)
        {
            var hasResumes = await _context.Resumes.AnyAsync(r => r.JobSeekerId == jobSeekerId);
            var hasApplications = await _context.Applications.AnyAsync(a => a.JobSeekerId == jobSeekerId);
            return hasResumes || hasApplications;
        }


        // ------------------- UTILITIES -------------------
        public async Task<bool> ExistsForUserAsync(Guid userId)
        {
            return await _context.JobSeekers.AnyAsync(js => js.UserId == userId);
        }
    }
}
