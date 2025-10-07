using System;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HireHub.API.Tests.Unit
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _notifRepo;
        private readonly Mock<IApplicationRepository> _appRepo;
        private readonly Mock<IEmailService> _emailSvc;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<ILogger<NotificationService>> _logger;
        private readonly NotificationService _sut;

        public NotificationServiceTests()
        {
            _notifRepo = new Mock<INotificationRepository>();
            _appRepo = new Mock<IApplicationRepository>();
            _emailSvc = new Mock<IEmailService>();
            _mapper = new Mock<IMapper>();
            _logger = new Mock<ILogger<NotificationService>>();

            _sut = new NotificationService(
                _notifRepo.Object,
                _appRepo.Object,
                _emailSvc.Object,
                _mapper.Object,
                _logger.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_Throws_NotFound_When_Missing()
        {
            var id = 999;
            _notifRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Notification?)null);

            Func<Task> act = async () => await _sut.GetByIdAsync(id);

            await act.Should().ThrowAsync<Exception>().Where(e => e.Message.Contains("not found"));
        }

        [Fact]
        public async Task NotifyApplicantByApplicationAsync_CreatesNotification_And_SendsEmail_IfRequested()
        {
            // Arrange
            var appId = 5;
            var employerUserId = Guid.NewGuid();
            var jobSeekerUserId = Guid.NewGuid();

            var application = new Application
            {
                ApplicationId = appId,
                JobSeeker = new JobSeeker
                {
                    JobSeekerId = Guid.NewGuid(),
                    User = new User { UserId = jobSeekerUserId, Email = "js@t.test" }
                },
                Job = new Job
                {
                    JobId = 1,
                    Employer = new Employer
                    {
                        EmployerId = Guid.NewGuid(),
                        User = new User { UserId = employerUserId, Email = "emp@t.test" }
                    }
                }
            };

            _appRepo.Setup(a => a.GetByIdWithDetailsAsync(appId)).ReturnsAsync(application);

            var createdNotification = new Notification { NotificationId = 7, UserId = jobSeekerUserId, Message = "m" };
            _notifRepo.Setup(n => n.AddAsync(It.IsAny<Notification>())).ReturnsAsync(createdNotification);
            _notifRepo.Setup(n => n.GetByIdAsync(createdNotification.NotificationId)).ReturnsAsync(createdNotification);

            _emailSvc.Setup(e => e.SendAsync(application.JobSeeker.User.Email, It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(true);
            _notifRepo.Setup(n => n.SetSentEmailAsync(createdNotification.NotificationId)).ReturnsAsync(true);

            var dto = new EmployerNotifyApplicantDto
            {
                ApplicationId = appId,
                Message = "Interview",
                Subject = "Interview",
                SendEmail = true
            };

            var mappedDto = new NotificationDto
            {
                NotificationId = createdNotification.NotificationId,
                UserId = createdNotification.UserId,
                Message = createdNotification.Message
            };

            _mapper.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>())).Returns(mappedDto);

            // Act
            var result = await _sut.NotifyApplicantByApplicationAsync(dto, employerUserId);

            // Assert
            result.Should().NotBeNull();
            result.NotificationId.Should().Be(createdNotification.NotificationId);
            _emailSvc.Verify(e => e.SendAsync(application.JobSeeker.User.Email, dto.Subject ?? It.IsAny<string>(), dto.Message), Times.Once);
            _notifRepo.Verify(n => n.SetSentEmailAsync(createdNotification.NotificationId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_CallsRepository_And_ReturnsMappedDto()
        {
            var createDto = new CreateNotificationDto { UserId = Guid.NewGuid(), Message = "hello" };
            var entity = new Notification { NotificationId = 1, UserId = createDto.UserId, Message = createDto.Message, IsRead = false, CreatedAt = DateTime.UtcNow };
            var outDto = new NotificationDto { NotificationId = 1, UserId = createDto.UserId, Message = createDto.Message };

            _mapper.Setup(m => m.Map<Notification>(createDto)).Returns(entity);
            _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).ReturnsAsync(entity);
            _notifRepo.Setup(r => r.GetByIdAsync(entity.NotificationId)).ReturnsAsync(entity);
            _mapper.Setup(m => m.Map<NotificationDto>(entity)).Returns(outDto);

            var res = await _sut.CreateAsync(createDto);

            res.Should().NotBeNull();
            res.NotificationId.Should().Be(entity.NotificationId);
            _notifRepo.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Throws_When_NotFound()
        {
            var id = 50;
            _notifRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Notification?)null);
            var dto = new UpdateNotificationDto { IsRead = true };

            Func<Task> act = async () => await _sut.UpdateAsync(id, dto);
            await act.Should().ThrowAsync<Exception>().Where(e => e.Message.Contains("not found"));
        }

        [Fact]
        public async Task MarkAsReadAsync_Returns_RepoResult()
        {
            var id = 33;
            _notifRepo.Setup(r => r.MarkAsReadAsync(id)).ReturnsAsync(true);

            var res = await _sut.MarkAsReadAsync(id);
            res.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_Throws_When_NotFound()
        {
            var id = 99;
            _notifRepo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            Func<Task> act = async () => await _sut.DeleteAsync(id);
            await act.Should().ThrowAsync<Exception>().Where(e => e.Message.Contains("not found"));
        }
    }
}
