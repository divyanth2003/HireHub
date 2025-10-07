
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
    public class EmployerService
    {
        private readonly IEmployerRepository _employerRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployerService> _logger;

        public EmployerService(
            IEmployerRepository employerRepository,
            IMapper mapper,
            ILogger<EmployerService> logger)
        {
            _employerRepository = employerRepository;
            _mapper = mapper;
            _logger = logger;
        }

       
        public async Task<IEnumerable<EmployerDto>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all employers");
            var employers = await _employerRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<EmployerDto>>(employers);
        }

        public async Task<EmployerDto> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting employer by id {EmployerId}", id);
            var employer = await _employerRepository.GetByIdAsync(id);
            if (employer == null)
            {
                _logger.LogWarning("Employer with id {EmployerId} not found", id);
                throw new NotFoundException($"Employer with id '{id}' not found.");
            }
            return _mapper.Map<EmployerDto>(employer);
        }

        public async Task<EmployerDto> GetByUserIdAsync(Guid userId)
        {
            _logger.LogInformation("Getting employer by user id {UserId}", userId);
            var employer = await _employerRepository.GetByUserIdAsync(userId);
            if (employer == null)
            {
                _logger.LogWarning("Employer with user id {UserId} not found", userId);
                throw new NotFoundException($"Employer with user id '{userId}' not found.");
            }
            return _mapper.Map<EmployerDto>(employer);
        }

        public async Task<IEnumerable<EmployerDto>> SearchByCompanyNameAsync(string companyName)
        {
            _logger.LogInformation("Searching employers with company name like {CompanyName}", companyName);
            var employers = await _employerRepository.SearchByCompanyNameAsync(companyName);
            return _mapper.Map<IEnumerable<EmployerDto>>(employers);
        }

        public async Task<EmployerDto> GetByJobIdAsync(int jobId)
        {
            _logger.LogInformation("Getting employer by job id {JobId}", jobId);
            var employer = await _employerRepository.GetByJobIdAsync(jobId);
            if (employer == null)
            {
                _logger.LogWarning("Employer not found for job {JobId}", jobId);
                throw new NotFoundException($"Employer for job id '{jobId}' not found.");
            }
            return _mapper.Map<EmployerDto>(employer);
        }

   
        public async Task<EmployerDto> CreateAsync(CreateEmployerDto dto)
        {
            _logger.LogInformation("Creating employer with company {CompanyName}", dto.CompanyName);

            var employer = _mapper.Map<Employer>(dto);
            employer.EmployerId = Guid.NewGuid();

            try
            {
                var created = await _employerRepository.AddAsync(employer);
                _logger.LogInformation("Employer created: {EmployerId}", created.EmployerId);
                return _mapper.Map<EmployerDto>(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employer {CompanyName}", dto.CompanyName);
                throw;
            }
        }

     
        public async Task<EmployerDto> UpdateAsync(Guid id, UpdateEmployerDto dto)
        {
            _logger.LogInformation("Updating employer {EmployerId}", id);

            var employer = await _employerRepository.GetByIdAsync(id);
            if (employer == null)
            {
                _logger.LogWarning("Employer {EmployerId} not found for update", id);
                throw new NotFoundException($"Employer with id '{id}' not found.");
            }

            _mapper.Map(dto, employer);

            try
            {
                var updated = await _employerRepository.UpdateAsync(employer);
                _logger.LogInformation("Employer {EmployerId} updated", id);
                return _mapper.Map<EmployerDto>(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employer {EmployerId}", id);
                throw;
            }
        }

       
        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting employer {EmployerId}", id);

            var deleted = await _employerRepository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Employer {EmployerId} not found for deletion", id);
                throw new NotFoundException($"Employer with id '{id}' not found.");
            }

            _logger.LogInformation("Employer {EmployerId} deleted", id);
            return true;
        }
    }
}
