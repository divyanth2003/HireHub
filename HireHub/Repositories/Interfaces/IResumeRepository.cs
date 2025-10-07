
using HireHub.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IResumeRepository
    {
        Task<IEnumerable<Resume>> GetAllAsync();
        Task<Resume?> GetByIdAsync(int id);
        Task<IEnumerable<Resume>> GetByJobSeekerAsync(Guid jobSeekerId);
        Task<Resume?> GetDefaultByJobSeekerAsync(Guid jobSeekerId);

        Task<Resume> AddAsync(Resume resume);
        Task<Resume> UpdateAsync(Resume resume);
        Task<bool> DeleteAsync(int id);
      
        Task<bool> HasDependentsAsync(int resumeId);


        Task SetDefaultAsync(Guid jobSeekerId, int resumeId);
        Task<bool> ExistsByNameAsync(Guid jobSeekerId, string resumeName);
    }
}
