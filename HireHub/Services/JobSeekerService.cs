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
    public class JobSeekerService
    {
        private readonly IJobSeekerRepository _jobSeekerRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<JobSeekerService> _logger;

        public JobSeekerService(
            IJobSeekerRepository jobSeekerRepository,
            IMapper mapper,
            ILogger<JobSeekerService> logger)
        {
            _jobSeekerRepository = jobSeekerRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<JobSeekerDto>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all jobseekers");
            var jobSeekers = await _jobSeekerRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<JobSeekerDto>>(jobSeekers);
        }

        public async Task<JobSeekerDto> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting jobseeker by id {JobSeekerId}", id);
            var js = await _jobSeekerRepository.GetByIdAsync(id);
            if (js == null)
            {
                _logger.LogWarning("JobSeeker with id {JobSeekerId} not found", id);
                throw new NotFoundException($"JobSeeker with id '{id}' not found.");
            }
            return _mapper.Map<JobSeekerDto>(js);
        }

        public async Task<JobSeekerDto> GetByUserIdAsync(Guid userId)
        {
            _logger.LogInformation("Getting jobseeker by user id {UserId}", userId);
            var js = await _jobSeekerRepository.GetByUserIdAsync(userId);
            if (js == null)
            {
                _logger.LogWarning("JobSeeker with user id {UserId} not found", userId);
                throw new NotFoundException($"JobSeeker with user id '{userId}' not found.");
            }
            return _mapper.Map<JobSeekerDto>(js);
        }

        public async Task<IEnumerable<JobSeekerDto>> SearchByCollegeAsync(string college)
        {
            _logger.LogInformation("Searching jobseekers by college {College}", college);
            var jobSeekers = await _jobSeekerRepository.SearchByCollegeAsync(college);
            return _mapper.Map<IEnumerable<JobSeekerDto>>(jobSeekers);
        }

        public async Task<IEnumerable<JobSeekerDto>> SearchBySkillAsync(string skill)
        {
            _logger.LogInformation("Searching jobseekers by skill {Skill}", skill);
            var jobSeekers = await _jobSeekerRepository.SearchBySkillAsync(skill);
            return _mapper.Map<IEnumerable<JobSeekerDto>>(jobSeekers);
        }

        // ------------------- CREATE -------------------
        public async Task<JobSeekerDto> CreateAsync(CreateJobSeekerDto dto)
        {
            _logger.LogInformation("Creating jobseeker for user {UserId}", dto.UserId);

            if (await _jobSeekerRepository.ExistsForUserAsync(dto.UserId))
            {
                _logger.LogWarning("Duplicate JobSeeker creation attempted for user {UserId}", dto.UserId);
                throw new DuplicateEmailException($"A JobSeeker profile already exists for user '{dto.UserId}'.");
                // Consider renaming this exception to DuplicateEntityException
            }

            var jobSeeker = _mapper.Map<JobSeeker>(dto);
            jobSeeker.JobSeekerId = Guid.NewGuid();

            try
            {
                var created = await _jobSeekerRepository.AddAsync(jobSeeker);
                _logger.LogInformation("JobSeeker created: {JobSeekerId}", created.JobSeekerId);
                return _mapper.Map<JobSeekerDto>(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating jobseeker for user {UserId}", dto.UserId);
                throw;
            }
        }

        // ------------------- UPDATE -------------------
        public async Task<JobSeekerDto> UpdateAsync(Guid id, UpdateJobSeekerDto dto)
        {
            _logger.LogInformation("Updating jobseeker {JobSeekerId}", id);

            var existing = await _jobSeekerRepository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("JobSeeker {JobSeekerId} not found for update", id);
                throw new NotFoundException($"JobSeeker with id '{id}' not found.");
            }

            _mapper.Map(dto, existing);

            try
            {
                var updated = await _jobSeekerRepository.UpdateAsync(existing);
                _logger.LogInformation("JobSeeker {JobSeekerId} updated", id);
                return _mapper.Map<JobSeekerDto>(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating jobseeker {JobSeekerId}", id);
                throw;
            }
        }

        // ------------------- DELETE -------------------
        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting jobseeker {JobSeekerId}", id);

            var deleted = await _jobSeekerRepository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("JobSeeker {JobSeekerId} not found for deletion", id);
                throw new NotFoundException($"JobSeeker with id '{id}' not found.");
            }

            _logger.LogInformation("JobSeeker {JobSeekerId} deleted", id);
            return true;
        }
    }
}
