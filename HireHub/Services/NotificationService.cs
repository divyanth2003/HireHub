using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;

namespace HireHub.API.Services
{
    public class NotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            IApplicationRepository applicationRepository,
            IEmailService emailService,
            IMapper mapper,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<NotificationDto>> GetByUserAsync(Guid userId)
        {
            _logger.LogInformation("Fetching notifications for user {UserId}", userId);
            var notifs = await _notificationRepository.GetByUserAsync(userId);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifs);
        }

        public async Task<IEnumerable<NotificationDto>> GetUnreadByUserAsync(Guid userId)
        {
            _logger.LogInformation("Fetching unread notifications for user {UserId}", userId);
            var notifs = await _notificationRepository.GetUnreadByUserAsync(userId);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifs);
        }

        public async Task<IEnumerable<NotificationDto>> GetRecentByUserAsync(Guid userId, int limit = 20)
        {
            _logger.LogInformation("Fetching recent {Limit} notifications for user {UserId}", limit, userId);
            var notifs = await _notificationRepository.GetRecentByUserAsync(userId, limit);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifs);
        }

        public async Task<NotificationDto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Getting notification {NotificationId}", id);
            var notif = await _notificationRepository.GetByIdAsync(id);
            if (notif == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found", id);
                throw new NotFoundException($"Notification with id '{id}' not found.");
            }

            return _mapper.Map<NotificationDto>(notif);
        }
        public async Task<NotificationDto> NotifyApplicantByApplicationAsync(EmployerNotifyApplicantDto dto, Guid employerUserId)
        {
            var app = await _applicationRepository.GetByIdWithDetailsAsync(dto.ApplicationId);
            if (app == null)
                throw new NotFoundException($"Application {dto.ApplicationId} not found.");

            // Ensure Job and Employer.User are loaded
            if (app.Job == null || app.Job.Employer == null || app.Job.Employer.User == null)
                throw new InvalidOperationException("Application or job/employer relation incomplete.");

            // Verify the logged-in employer owns the job (job.Employer.User.UserId is the employer user)
            if (app.Job.Employer.User.UserId != employerUserId)
                throw new ForbiddenException("Not authorized to message this applicant.");

            // Create notification for jobseeker's user
            var notif = new Notification
            {
                UserId = app.JobSeeker.User.UserId,
                Subject = dto.Subject,
                Message = dto.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _notificationRepository.AddAsync(notif);

            // Optionally send email
            if (dto.SendEmail && !string.IsNullOrWhiteSpace(app.JobSeeker.User.Email))
            {
                var sent = await _emailService.SendAsync(
                    app.JobSeeker.User.Email,
                    dto.Subject ?? "Message from employer",
                    dto.Message
                );
                if (sent)
                    await _notificationRepository.SetSentEmailAsync(created.NotificationId);
            }

            var createdWithNav = await _notificationRepository.GetByIdAsync(created.NotificationId);
            return _mapper.Map<NotificationDto>(createdWithNav ?? created);
        }



        // ------------------- CREATE -------------------
        public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
        {
            _logger.LogInformation("Creating notification for user {UserId}", dto.UserId);

            var entity = _mapper.Map<Notification>(dto);
            entity.IsRead = false;
            entity.CreatedAt = DateTime.UtcNow;
            entity.SentEmail = false;

            var created = await _notificationRepository.AddAsync(entity);
            var createdWithNav = await _notificationRepository.GetByIdAsync(created.NotificationId) ?? created;

            _logger.LogInformation("Notification created {NotificationId} for user {UserId}",
                created.NotificationId, created.UserId);

            return _mapper.Map<NotificationDto>(createdWithNav);
        }

        // ------------------- UPDATE -------------------
        public async Task<NotificationDto?> UpdateAsync(int id, UpdateNotificationDto dto)
        {
            _logger.LogInformation("Updating notification {NotificationId}", id);

            var notif = await _notificationRepository.GetByIdAsync(id);
            if (notif == null)
                throw new NotFoundException($"Notification with id '{id}' not found.");

            notif.IsRead = dto.IsRead;
            if (!string.IsNullOrWhiteSpace(dto.Message))
                notif.Message = dto.Message;

            var updated = await _notificationRepository.UpdateAsync(notif);
            var updatedWithNav = await _notificationRepository.GetByIdAsync(updated.NotificationId) ?? updated;

            return _mapper.Map<NotificationDto>(updatedWithNav);
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            _logger.LogInformation("Marking notification {NotificationId} as read", id);
            return await _notificationRepository.MarkAsReadAsync(id);
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);
            return await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        // ------------------- DELETE -------------------
        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting notification {NotificationId}", id);
            var deleted = await _notificationRepository.DeleteAsync(id);
            if (!deleted)
                throw new NotFoundException($"Notification with id '{id}' not found.");
            return true;
        }
    }
}
