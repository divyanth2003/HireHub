using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.Data;
using Microsoft.EntityFrameworkCore;

namespace HireHub.API.Repositories.Implementations
{
    public class EmployerRepository : IEmployerRepository
    {
        private readonly HireHubContext _context;

        public EmployerRepository(HireHubContext context)
        {
            _context = context;
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<Employer>> GetAllAsync()
        {
            return await _context.Employers
                .Include(e => e.User)
                .Include(e => e.Jobs)
                .ToListAsync();
        }

        public async Task<Employer?> GetByIdAsync(Guid id)
        {
            return await _context.Employers
                .Include(e => e.User)
                .Include(e => e.Jobs)
                .FirstOrDefaultAsync(e => e.EmployerId == id);
        }

        public async Task<Employer?> GetByUserIdAsync(Guid userId)
        {
            return await _context.Employers
                .Include(e => e.User)
                .Include(e => e.Jobs)
                .FirstOrDefaultAsync(e => e.UserId == userId);
        }
        public async Task<Employer?> GetByJobIdAsync(int jobId)
        {
            return await _context.Jobs
                .Where(j => j.JobId == jobId)
                .Include(j => j.Employer)
                    .ThenInclude(e => e.User)
                .Select(j => j.Employer)
                .FirstOrDefaultAsync();
        }


        public async Task<IEnumerable<Employer>> SearchByCompanyNameAsync(string companyName)
        {
            return await _context.Employers
                .Where(e => e.CompanyName.Contains(companyName))
                .Include(e => e.User)
                .Include(e => e.Jobs)
                .ToListAsync();
        }

        // ------------------- ADD/UPDATE/DELETE -------------------
        public async Task<Employer> AddAsync(Employer employer)
        {
            _context.Employers.Add(employer);
            await _context.SaveChangesAsync();
            return employer;
        }

        public async Task<Employer> UpdateAsync(Employer employer)
        {
            _context.Employers.Update(employer);
            await _context.SaveChangesAsync();
            return employer;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var employer = await _context.Employers.FindAsync(id);
            if (employer == null) return false;

            _context.Employers.Remove(employer);
            await _context.SaveChangesAsync();
            return true;
        }

        // ------------------- UTILITIES -------------------
        public async Task<bool> ExistsByCompanyNameAsync(string companyName)
        {
            return await _context.Employers.AnyAsync(e => e.CompanyName == companyName);
        }
    }
}
