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
    public class JobService
    {
        private readonly IJobRepository _jobRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<JobService> _logger;

        public JobService(
            IJobRepository jobRepository,
            IMapper mapper,
            ILogger<JobService> logger)
        {
            _jobRepository = jobRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<JobDto>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all jobs");
            var jobs = await _jobRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<JobDto>>(jobs);
        }

        public async Task<JobDto> GetByIdAsync(int id)
        {
            _logger.LogInformation("Getting job by id {JobId}", id);
            var job = await _jobRepository.GetByIdAsync(id);
            if (job == null)
            {
                _logger.LogWarning("Job with id {JobId} not found", id);
                throw new NotFoundException($"Job with id '{id}' not found.");
            }
            return _mapper.Map<JobDto>(job);
        }

        public async Task<IEnumerable<JobDto>> GetByEmployerAsync(Guid employerId)
        {
            _logger.LogInformation("Getting jobs for employer {EmployerId}", employerId);
            var jobs = await _jobRepository.GetByEmployerAsync(employerId);
            return _mapper.Map<IEnumerable<JobDto>>(jobs);
        }

        public async Task<IEnumerable<JobDto>> SearchByTitleAsync(string title)
        {
            _logger.LogInformation("Searching jobs by title {Title}", title);
            var jobs = await _jobRepository.SearchByTitleAsync(title);
            return _mapper.Map<IEnumerable<JobDto>>(jobs);
        }

        public async Task<IEnumerable<JobDto>> SearchByLocationAsync(string location)
        {
            _logger.LogInformation("Searching jobs by location {Location}", location);
            var jobs = await _jobRepository.SearchByLocationAsync(location);
            return _mapper.Map<IEnumerable<JobDto>>(jobs);
        }

        public async Task<IEnumerable<JobDto>> SearchBySkillAsync(string skill)
        {
            _logger.LogInformation("Searching jobs by skill {Skill}", skill);
            var jobs = await _jobRepository.SearchBySkillAsync(skill);
            return _mapper.Map<IEnumerable<JobDto>>(jobs);
        }

        // ------------------- CREATE -------------------
        public async Task<JobDto> CreateAsync(CreateJobDto dto)
        {
            _logger.LogInformation("Creating job {@CreateJobDto}", dto);

            var job = _mapper.Map<Job>(dto);
            job.CreatedAt = DateTime.UtcNow;
            job.Status = string.IsNullOrWhiteSpace(job.Status) ? "Open" : job.Status;

            try
            {
                var created = await _jobRepository.AddAsync(job);
                _logger.LogInformation("Job created: {JobId}", created.JobId);
                return _mapper.Map<JobDto>(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job {@CreateJobDto}", dto);
                throw;
            }
        }

        // ------------------- UPDATE -------------------
        public async Task<JobDto> UpdateAsync(int id, UpdateJobDto dto)
        {
            _logger.LogInformation("Updating job {JobId}", id);

            var job = await _jobRepository.GetByIdAsync(id);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found for update", id);
                throw new NotFoundException($"Job with id '{id}' not found.");
            }

            _mapper.Map(dto, job);

            try
            {
                var updated = await _jobRepository.UpdateAsync(job);
                _logger.LogInformation("Job {JobId} updated", id);
                return _mapper.Map<JobDto>(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job {JobId}", id);
                throw;
            }
        }

        // ------------------- DELETE -------------------
        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting job {JobId}", id);

            var deleted = await _jobRepository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Job {JobId} not found for deletion", id);
                throw new NotFoundException($"Job with id '{id}' not found.");
            }

            _logger.LogInformation("Job {JobId} deleted", id);
            return true;
        }
    }
}
