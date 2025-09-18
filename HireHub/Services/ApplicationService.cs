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
    public class ApplicationService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ApplicationService> _logger;

        public ApplicationService(
            IApplicationRepository applicationRepository,
            IMapper mapper,
            ILogger<ApplicationService> logger)
        {
            _applicationRepository = applicationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<ApplicationDto>> GetAllAsync()
        {
            var apps = await _applicationRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ApplicationDto>>(apps);
        }

        public async Task<ApplicationDto?> GetByIdAsync(int id)
        {
            var app = await _applicationRepository.GetByIdAsync(id);
            if (app == null)
                throw new NotFoundException($"Application with id '{id}' not found.");

            return _mapper.Map<ApplicationDto>(app);
        }

        public async Task<IEnumerable<ApplicationDto>> GetByJobAsync(int jobId)
        {
            var apps = await _applicationRepository.GetByJobAsync(jobId);
            return _mapper.Map<IEnumerable<ApplicationDto>>(apps);
        }

        public async Task<IEnumerable<ApplicationDto>> GetByJobSeekerAsync(Guid jobSeekerId)
        {
            var apps = await _applicationRepository.GetByJobSeekerAsync(jobSeekerId);
            return _mapper.Map<IEnumerable<ApplicationDto>>(apps);
        }

        public async Task<IEnumerable<ApplicationDto>> GetShortlistedByJobAsync(int jobId)
        {
            var apps = await _applicationRepository.GetShortlistedByJobAsync(jobId);
            return _mapper.Map<IEnumerable<ApplicationDto>>(apps);
        }

        public async Task<IEnumerable<ApplicationDto>> GetWithInterviewAsync(int jobId)
        {
            var apps = await _applicationRepository.GetWithInterviewAsync(jobId);
            return _mapper.Map<IEnumerable<ApplicationDto>>(apps);
        }

        // ------------------- CREATE -------------------
        public async Task<ApplicationDto> CreateAsync(CreateApplicationDto dto)
        {
            var entity = _mapper.Map<Application>(dto);
            entity.Status = "Applied";
            entity.AppliedAt = DateTime.UtcNow;

            var created = await _applicationRepository.AddAsync(entity);

            var createdWithNav = await _applicationRepository.GetByIdAsync(created.ApplicationId) ?? created;
            return _mapper.Map<ApplicationDto>(createdWithNav);
        }

        // ------------------- UPDATE -------------------
        public async Task<ApplicationDto?> UpdateAsync(int id, UpdateApplicationDto dto)
        {
            var entity = await _applicationRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Application with id '{id}' not found.");

            _mapper.Map(dto, entity);

            var updated = await _applicationRepository.UpdateAsync(entity);

            var updatedWithNav = await _applicationRepository.GetByIdAsync(updated.ApplicationId) ?? updated;
            return _mapper.Map<ApplicationDto>(updatedWithNav);
        }

        // ------------------- DELETE -------------------
        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _applicationRepository.DeleteAsync(id);
            if (!deleted)
                throw new NotFoundException($"Application with id '{id}' not found.");

            return true;
        }

        // ------------------- UTILITIES -------------------
        public async Task<ApplicationDto?> MarkReviewedAsync(int appId, string? notes = null)
        {
            var app = await _applicationRepository.MarkReviewedAsync(appId, notes);
            if (app == null)
                throw new NotFoundException($"Application with id '{appId}' not found.");

            return _mapper.Map<ApplicationDto>(app);
        }
    }
}
