using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using HireHub.API.Exceptions;

namespace HireHub.API.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _notifRepo;
        private readonly Mock<IApplicationRepository> _appRepo;
        private readonly Mock<IEmailService> _email;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<ILogger<NotificationService>> _logger;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _notifRepo = new Mock<INotificationRepository>();
            _appRepo = new Mock<IApplicationRepository>();
            _email = new Mock<IEmailService>();
            _mapper = new Mock<IMapper>();
            _logger = new Mock<ILogger<NotificationService>>();

            _service = new NotificationService(
                _notifRepo.Object,
                _appRepo.Object,
                _email.Object,
                _mapper.Object,
                _logger.Object);
        }

        [Fact]
        public async Task GetByUserAsync_ReturnsMappedDtos()
        {
            var uid = Guid.NewGuid();
            var list = new List<Notification> { new Notification { NotificationId = 1, UserId = uid } };
            var outList = new List<NotificationDto> { new NotificationDto { NotificationId = 1 } };

            _notifRepo.Setup(r => r.GetByUserAsync(uid)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<IEnumerable<NotificationDto>>(list)).Returns(outList);

            var res = await _service.GetByUserAsync(uid);

            res.Should().BeEquivalentTo(outList);
        }

        [Fact]
        public async Task GetByIdAsync_ThrowsNotFound_WhenMissing()
        {
            _notifRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Notification?)null);

            Func<Task> act = async () => await _service.GetByIdAsync(99);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Notification with id '99' not found.");
        }

        [Fact]
        public async Task CreateAsync_SendsEmail_WhenSendEmailTrueAndEmailAvailable()
        {
            var userId = Guid.NewGuid();
            var inDto = new CreateNotificationDto { UserId = userId, Message = "Hello", Subject = "S", SendEmail = true };
            var entity = new Notification { NotificationId = 5, UserId = userId, Message = "Hello" };
            var entityWithUser = new Notification { NotificationId = 5, UserId = userId, Message = "Hello", User = new User { Email = "a@b.com" } };
            var outDto = new NotificationDto { NotificationId = 5, Message = "Hello" };

            _mapper.Setup(m => m.Map<Notification>(inDto)).Returns(entity);
            _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).ReturnsAsync(entity);
            _notifRepo.Setup(r => r.GetByIdAsync(entity.NotificationId)).ReturnsAsync(entityWithUser);
            _email.Setup(e => e.SendAsync(entityWithUser.User.Email, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _notifRepo.Setup(r => r.SetSentEmailAsync(entity.NotificationId)).ReturnsAsync(true);
            _mapper.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>())).Returns(outDto);

            var res = await _service.CreateAsync(inDto);

            res.Should().BeEquivalentTo(outDto);
            _email.Verify(e => e.SendAsync(entityWithUser.User.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _notifRepo.Verify(r => r.SetSentEmailAsync(entity.NotificationId), Times.Once);
        }

        [Fact]
        public async Task NotifyApplicantByApplicationAsync_ThrowsNotFound_IfApplicationMissing()
        {
            var dto = new EmployerNotifyApplicantDto { ApplicationId = 100, Message = "msg" };
            _appRepo.Setup(r => r.GetByIdWithDetailsAsync(100)).ReturnsAsync((Application?)null);

            Func<Task> act = async () => await _service.NotifyApplicantByApplicationAsync(dto, Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Application 100 not found.");
        }

        [Fact]
        public async Task NotifyApplicantByApplicationAsync_ThrowsForbidden_WhenNotOwner()
        {
            var dto = new EmployerNotifyApplicantDto { ApplicationId = 200, Message = "msg", Subject = "s", SendEmail = false };
            var app = new Application
            {
                ApplicationId = 200,
                Job = new Job { Employer = new Employer { User = new User { UserId = Guid.NewGuid() } } },
                JobSeeker = new JobSeeker { User = new User { UserId = Guid.NewGuid(), Email = "js@x.com" } }
            };

            _appRepo.Setup(r => r.GetByIdWithDetailsAsync(200)).ReturnsAsync(app);

            Func<Task> act = async () => await _service.NotifyApplicantByApplicationAsync(dto, Guid.NewGuid()); // different user

            await act.Should().ThrowAsync<ForbiddenException>().WithMessage("Not authorized to message this applicant.");
        }

        [Fact]
        public async Task NotifyApplicantByApplicationAsync_CreatesNotification_AndOptionallySendsEmail()
        {
            var employerUser = Guid.NewGuid();
            var dto = new EmployerNotifyApplicantDto { ApplicationId = 300, Message = "You are selected", Subject = "Interview", SendEmail = true };

            var app = new Application
            {
                ApplicationId = 300,
                Job = new Job { Title = "Dev", Employer = new Employer { CompanyName = "C", User = new User { UserId = employerUser } } },
                JobSeeker = new JobSeeker { User = new User { UserId = Guid.NewGuid(), Email = "cand@x.com", FullName = "Jane" } }
            };

            var notifEntity = new Notification { NotificationId = 400, UserId = app.JobSeeker.User.UserId, Message = dto.Message };

            _appRepo.Setup(r => r.GetByIdWithDetailsAsync(300)).ReturnsAsync(app);
            _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).ReturnsAsync(notifEntity);
            _notifRepo.Setup(r => r.GetByIdAsync(notifEntity.NotificationId)).ReturnsAsync(notifEntity);
            _email.Setup(e => e.SendAsync(app.JobSeeker.User.Email, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _notifRepo.Setup(r => r.SetSentEmailAsync(notifEntity.NotificationId)).ReturnsAsync(true);

            var outDto = new NotificationDto { NotificationId = 400 };
            _mapper.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>())).Returns(outDto);

            var res = await _service.NotifyApplicantByApplicationAsync(dto, employerUser);

            res.Should().BeEquivalentTo(outDto);
            _email.Verify(e => e.SendAsync(app.JobSeeker.User.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsNotFound_WhenMissing()
        {
            _notifRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Notification?)null);
            Func<Task> act = async () => await _service.UpdateAsync(999, new UpdateNotificationDto { IsRead = true });
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Notification with id '999' not found.");
        }

        [Fact]
        public async Task MarkAsRead_ReturnsTrue_WhenRepositoryReturnsTrue()
        {
            _notifRepo.Setup(r => r.MarkAsReadAsync(11)).ReturnsAsync(true);
            var res = await _service.MarkAsReadAsync(11);
            res.Should().BeTrue();
        }

        [Fact]
        public async Task MarkAllAsRead_ReturnsCount()
        {
            var userId = Guid.NewGuid();
            _notifRepo.Setup(r => r.MarkAllAsReadAsync(userId)).ReturnsAsync(3);

            var res = await _service.MarkAllAsReadAsync(userId);
            res.Should().Be(3);
        }

        [Fact]
        public async Task DeleteAsync_ThrowsNotFound_WhenRepoReturnsFalse()
        {
            _notifRepo.Setup(r => r.DeleteAsync(77)).ReturnsAsync(false);
            Func<Task> act = async () => await _service.DeleteAsync(77);
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Notification with id '77' not found.");
        }

        [Fact]
        public async Task DeleteAsync_ReturnsTrue_WhenDeleted()
        {
            _notifRepo.Setup(r => r.DeleteAsync(88)).ReturnsAsync(true);
            var res = await _service.DeleteAsync(88);
            res.Should().BeTrue();
        }
    }
}
