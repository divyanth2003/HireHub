using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.Data;
using Microsoft.EntityFrameworkCore;

namespace HireHub.API.Repositories.Implementations
{
    public class JobRepository : IJobRepository
    {
        private readonly HireHubContext _context;

        public JobRepository(HireHubContext context)
        {
            _context = context;
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<Job>> GetAllAsync()
        {
            return await _context.Jobs
                .Include(j => j.Employer)
                .ThenInclude(e => e.User) // include employer's user info
                .ToListAsync();
        }

        public async Task<Job?> GetByIdAsync(int id)
        {
            return await _context.Jobs
                .Include(j => j.Employer)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(j => j.JobId == id);
        }

        public async Task<IEnumerable<Job>> GetByEmployerAsync(Guid employerId)
        {
            return await _context.Jobs
                .Where(j => j.EmployerId == employerId)
                .Include(j => j.Employer)
                .ThenInclude(e => e.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> SearchByTitleAsync(string title)
        {
            return await _context.Jobs
                .Where(j => j.Title.Contains(title))
                .Include(j => j.Employer)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> SearchByLocationAsync(string location)
        {
            return await _context.Jobs
                .Where(j => j.Location != null && j.Location.Contains(location))
                .Include(j => j.Employer)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> SearchBySkillAsync(string skill)
        {
            return await _context.Jobs
                .Where(j => j.SkillsRequired != null && j.SkillsRequired.Contains(skill))
                .Include(j => j.Employer)
                .ToListAsync();
        }

        // ------------------- ADD/UPDATE/DELETE -------------------
        public async Task<Job> AddAsync(Job job)
        {
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<Job> UpdateAsync(Job job)
        {
            _context.Jobs.Update(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return false;

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
