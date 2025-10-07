
using AutoMapper;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HireHub.API.Services
{
    public class ResumeService
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ResumeService> _logger;
        private readonly string _wwwroot;
        private readonly string _uploadsRoot;

        public ResumeService(
            IResumeRepository resumeRepository,
            IMapper mapper,
            ILogger<ResumeService> logger)
        {
            _resumeRepository = resumeRepository;
            _mapper = mapper;
            _logger = logger;

            // compute once
            _wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            _uploadsRoot = Path.Combine(_wwwroot, "Uploads");
        }

  
        public async Task<IEnumerable<ResumeDto>> GetAllAsync()
        {
            var resumes = await _resumeRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ResumeDto>>(resumes);
        }

        public async Task<ResumeDto?> GetByIdAsync(int id)
        {
            var resume = await _resumeRepository.GetByIdAsync(id);
            if (resume == null)
                throw new NotFoundException($"Resume with id '{id}' not found.");

            return _mapper.Map<ResumeDto>(resume);
        }

        public async Task<IEnumerable<ResumeDto>> GetByJobSeekerAsync(Guid jobSeekerId)
        {
            var resumes = await _resumeRepository.GetByJobSeekerAsync(jobSeekerId);
            return _mapper.Map<IEnumerable<ResumeDto>>(resumes);
        }

        public async Task<ResumeDto?> GetDefaultByJobSeekerAsync(Guid jobSeekerId)
        {
            var resume = await _resumeRepository.GetDefaultByJobSeekerAsync(jobSeekerId);
            if (resume == null) return null;
            return _mapper.Map<ResumeDto>(resume);
        }

        public async Task<ResumeDto> CreateAsync(CreateResumeDto dto)
        {
           
            if (await _resumeRepository.ExistsByNameAsync(dto.JobSeekerId, dto.ResumeName))
                throw new DuplicateEmailException($"Resume '{dto.ResumeName}' already exists for this JobSeeker.");

            var resume = _mapper.Map<Resume>(dto);
            resume.UpdatedAt = DateTime.UtcNow;

            var created = await _resumeRepository.AddAsync(resume);

            if (created.IsDefault)
                await _resumeRepository.SetDefaultAsync(created.JobSeekerId, created.ResumeId);

            return _mapper.Map<ResumeDto>(created);
        }

        
        public async Task<ResumeDto?> UpdateAsync(int id, UpdateResumeDto dto)
        {
            var resume = await _resumeRepository.GetByIdAsync(id);
            if (resume == null)
                throw new NotFoundException($"Resume with id '{id}' not found.");

            _mapper.Map(dto, resume);
            resume.UpdatedAt = DateTime.UtcNow;

            var updated = await _resumeRepository.UpdateAsync(resume);

            if (updated.IsDefault)
                await _resumeRepository.SetDefaultAsync(updated.JobSeekerId, updated.ResumeId);

            return _mapper.Map<ResumeDto>(updated);
        }

      
        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting resume {ResumeId}", id);

           
            var resume = await _resumeRepository.GetByIdAsync(id);
            if (resume == null)
            {
                _logger.LogWarning("Resume {ResumeId} not found", id);
                throw new NotFoundException($"Resume with id '{id}' not found.");
            }

         
            try
            {
                if (!string.IsNullOrWhiteSpace(resume.FilePath))
                {
               
                    var relative = resume.FilePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                    var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var fullPath = Path.Combine(webRoot, relative);

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        _logger.LogInformation("Deleted file {FilePath} for resume {ResumeId}", fullPath, id);
                    }
                    else
                    {
                        _logger.LogInformation("File {FilePath} not found while deleting resume {ResumeId}", fullPath, id);
                    }
                }
            }
            catch (Exception ex)
            {
           
                _logger.LogError(ex, "Failed deleting file for resume {ResumeId}, continuing to remove DB record", id);
            }

           
            try
            {
                var deleted = await _resumeRepository.DeleteAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("Resume {ResumeId} delete returned false", id);
                    throw new NotFoundException($"Resume with id '{id}' not found.");
                }
                _logger.LogInformation("Resume {ResumeId} deleted", id);
                return true;
            }
            catch (DbUpdateException dbex)
            {
          
                var inner = dbex.InnerException?.Message ?? dbex.Message;
                _logger.LogWarning(dbex, "DB delete failed for resume {ResumeId} because of dependent data: {DbMessage}", id, inner);

              
                throw new ConflictException($"Cannot delete resume {id}: dependent data exists. Delete dependents first.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting resume {ResumeId}", id);
                throw; 
            }
        }

   
        public async Task<bool> SetDefaultAsync(Guid jobSeekerId, int resumeId)
        {
            var resume = await _resumeRepository.GetByIdAsync(resumeId);
            if (resume == null || resume.JobSeekerId != jobSeekerId)
                throw new NotFoundException($"Resume with id '{resumeId}' not found for jobSeeker {jobSeekerId}.");

            await _resumeRepository.SetDefaultAsync(jobSeekerId, resumeId);
            return true;
        }
    }
}