using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.Data;
using Microsoft.EntityFrameworkCore;

namespace HireHub.API.Repositories.Implementations
{
    public class ResumeRepository : IResumeRepository
    {
        private readonly HireHubContext _context;

        public ResumeRepository(HireHubContext context)
        {
            _context = context;
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<Resume>> GetAllAsync()
        {
            return await _context.Resumes
                .Include(r => r.JobSeeker)
                .ThenInclude(js => js.User)
                .ToListAsync();
        }

        public async Task<Resume?> GetByIdAsync(int id)
        {
            return await _context.Resumes
                .Include(r => r.JobSeeker)
                .ThenInclude(js => js.User)
                .FirstOrDefaultAsync(r => r.ResumeId == id);
        }

        public async Task<IEnumerable<Resume>> GetByJobSeekerAsync(Guid jobSeekerId)
        {
            return await _context.Resumes
                .Where(r => r.JobSeekerId == jobSeekerId)
                .Include(r => r.JobSeeker)
                .ThenInclude(js => js.User)
                .ToListAsync();
        }

        public async Task<Resume?> GetDefaultByJobSeekerAsync(Guid jobSeekerId)
        {
            return await _context.Resumes
                .Where(r => r.JobSeekerId == jobSeekerId && r.IsDefault)
                .Include(r => r.JobSeeker)
                .ThenInclude(js => js.User)
                .FirstOrDefaultAsync();
        }

        // ------------------- ADD/UPDATE/DELETE -------------------
        public async Task<Resume> AddAsync(Resume resume)
        {
            _context.Resumes.Add(resume);
            await _context.SaveChangesAsync();
            return resume;
        }

        public async Task<Resume> UpdateAsync(Resume resume)
        {
            _context.Resumes.Update(resume);
            await _context.SaveChangesAsync();
            return resume;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var resume = await _context.Resumes.FindAsync(id);
            if (resume == null) return false;

            _context.Resumes.Remove(resume);
            await _context.SaveChangesAsync();
            return true;
        }
        // Repositories/Implementations/ResumeRepository.cs
        public async Task<bool> HasDependentsAsync(int resumeId)
        {
            // check Applications table
            return await _context.Applications.AnyAsync(a => a.ResumeId == resumeId);
        }


        // ------------------- UTILITIES -------------------
        public async Task SetDefaultAsync(Guid jobSeekerId, int resumeId)
        {
            // unset all other resumes for this jobseeker
            var resumes = await _context.Resumes
                .Where(r => r.JobSeekerId == jobSeekerId)
                .ToListAsync();

            foreach (var r in resumes)
            {
                r.IsDefault = (r.ResumeId == resumeId);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(Guid jobSeekerId, string resumeName)
        {
            return await _context.Resumes
                .AnyAsync(r => r.JobSeekerId == jobSeekerId && r.ResumeName == resumeName);
        }
    }
}
