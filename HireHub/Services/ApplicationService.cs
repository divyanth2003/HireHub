using AutoMapper;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services; 
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HireHub.API.Services
{
    public class ApplicationService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ApplicationService> _logger;
        private readonly NotificationService _notificationService; 

        public ApplicationService(
            IApplicationRepository applicationRepository,
            IMapper mapper,
            ILogger<ApplicationService> logger,
            NotificationService notificationService)
        {
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

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

        public async Task<ApplicationDto> CreateAsync(CreateApplicationDto dto)
        {
            var entity = _mapper.Map<Application>(dto);
            entity.Status = "Applied";
            entity.AppliedAt = DateTime.UtcNow;

            var created = await _applicationRepository.AddAsync(entity);

            
            var createdWithDetails = await TryGetAppWithDetailsAsync(created.ApplicationId) ?? created;

           
            try
            {
                var employerUserId = createdWithDetails?.Job?.Employer?.User?.UserId;
                var applicantName = createdWithDetails?.JobSeeker?.User?.FullName
                                    ?? createdWithDetails?.JobSeeker?.User?.UserId.ToString()
                                    ?? "A candidate";
                var jobTitle = createdWithDetails?.Job?.Title ?? "your job";

                if (employerUserId.HasValue && employerUserId.Value != Guid.Empty)
                {
                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId = employerUserId.Value,
                        Subject = $"New applicant for {jobTitle}",
                        Message = $"{applicantName} has applied for '{jobTitle}'.",
                        SendEmail = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create 'application submitted' notification for application {AppId}", created.ApplicationId);
            }

            var createdWithNav = await _applicationRepository.GetByIdAsync(created.ApplicationId) ?? created;
            return _mapper.Map<ApplicationDto>(createdWithNav);
        }

       
        public async Task<ApplicationDto?> UpdateAsync(int id, UpdateApplicationDto dto)
        {
            var entity = await _applicationRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Application with id '{id}' not found.");

           
            var oldStatus = entity.Status;

            
            _mapper.Map(dto, entity);

            var updated = await _applicationRepository.UpdateAsync(entity);

      
            var updatedWithDetails = await TryGetAppWithDetailsAsync(updated.ApplicationId) ?? updated;

            
            try
            {
                var newStatus = updatedWithDetails?.Status ?? string.Empty;
                if (!string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
                {
                    var jobSeekerUserId = updatedWithDetails?.JobSeeker?.User?.UserId;
                    var jobTitle = updatedWithDetails?.Job?.Title ?? "your application";
                    var employerName = updatedWithDetails?.Job?.Employer?.CompanyName
                                       ?? updatedWithDetails?.Job?.Employer?.User?.FullName
                                       ?? "the employer";

                    if (jobSeekerUserId.HasValue && jobSeekerUserId.Value != Guid.Empty)
                    {
                        string subject = $"Update: {jobTitle}";
                        string message = $"Your application status changed to '{newStatus}' for {jobTitle}.";

                        if (newStatus.Equals("Shortlisted", StringComparison.OrdinalIgnoreCase))
                        {
                            subject = $"You are shortlisted for {jobTitle}";
                            message = $"Congratulations! You have been shortlisted for {jobTitle} at {employerName}. Please check your application for details.";
                        }
                        else if (newStatus.IndexOf("Interview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 newStatus.Equals("Interview Scheduled", StringComparison.OrdinalIgnoreCase))
                        {
                            
                            var interviewInfo = string.Empty;
                            try
                            {
                                var dtProp = updatedWithDetails.GetType().GetProperty("InterviewDate");
                                if (dtProp != null)
                                {
                                    var dtVal = dtProp.GetValue(updatedWithDetails) as DateTime?;
                                    if (dtVal.HasValue)
                                        interviewInfo = $" Interview scheduled on {dtVal.Value.ToLocalTime():f}.";
                                }
                            }
                            catch { }

                            subject = $"Interview scheduled for {jobTitle}";
                            message = $"Your interview for {jobTitle} at {employerName} is scheduled.{interviewInfo}";
                        }
                        else if (newStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                        {
                            subject = $"Application update: {jobTitle}";
                            message = $"We’re sorry — your application for {jobTitle} at {employerName} was not selected.";
                        }

                   
                        await _notificationService.CreateAsync(new CreateNotificationDto
                        {
                            UserId = jobSeekerUserId.Value,
                            Subject = subject,
                            Message = message,
                            SendEmail = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification after application status change for app {AppId}", id);
            }

            var updatedWithNav = await _applicationRepository.GetByIdAsync(updated.ApplicationId) ?? updated;
            return _mapper.Map<ApplicationDto>(updatedWithNav);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var deleted = await _applicationRepository.DeleteAsync(id);
            if (!deleted)
                throw new NotFoundException($"Application with id '{id}' not found.");

            return true;
        }

        
        public async Task<ApplicationDto?> MarkReviewedAsync(int appId, string? notes = null)
        {
            var app = await _applicationRepository.MarkReviewedAsync(appId, notes);
            if (app == null)
                throw new NotFoundException($"Application with id '{appId}' not found.");

            return _mapper.Map<ApplicationDto>(app);
        }

       
        private async Task<Application?> TryGetAppWithDetailsAsync(int applicationId)
        {
            try
            {
                var method = _applicationRepository.GetType().GetMethod("GetByIdWithDetailsAsync");
                if (method != null)
                {
                    var task = (Task)method.Invoke(_applicationRepository, new object[] { applicationId });
                    await task.ConfigureAwait(false);
                    var resultProp = task.GetType().GetProperty("Result");
                    return resultProp?.GetValue(task) as Application;
                }

                
                return await _applicationRepository.GetByIdAsync(applicationId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load application details for {AppId}", applicationId);
                return await _applicationRepository.GetByIdAsync(applicationId);
            }
        }
    }
}