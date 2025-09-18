using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Exceptions;

namespace HireHub.API.Services
{
    public class ResumeService
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ResumeService> _logger;

        public ResumeService(
            IResumeRepository resumeRepository,
            IMapper mapper,
            ILogger<ResumeService> logger)
        {
            _resumeRepository = resumeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // ------------------- GET -------------------
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

        // ------------------- CREATE -------------------
        public async Task<ResumeDto> CreateAsync(CreateResumeDto dto)
        {
            // Check duplicate resume name
            if (await _resumeRepository.ExistsByNameAsync(dto.JobSeekerId, dto.ResumeName))
                throw new DuplicateEmailException($"Resume '{dto.ResumeName}' already exists for this JobSeeker.");

            var resume = _mapper.Map<Resume>(dto);
            resume.UpdatedAt = DateTime.UtcNow;

            var created = await _resumeRepository.AddAsync(resume);

            // If marked as default, enforce rule
            if (created.IsDefault)
                await _resumeRepository.SetDefaultAsync(created.JobSeekerId, created.ResumeId);

            return _mapper.Map<ResumeDto>(created);
        }

        // ------------------- UPDATE -------------------
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

        // ------------------- DELETE -------------------
        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _resumeRepository.DeleteAsync(id);
            if (!deleted)
                throw new NotFoundException($"Resume with id '{id}' not found.");

            return true;
        }

        // ------------------- UTILITIES -------------------
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
