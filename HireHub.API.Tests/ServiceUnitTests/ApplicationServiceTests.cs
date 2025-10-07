using AutoMapper;
using FluentAssertions;
using HireHub.API.Controllers;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HireHub.API.Tests.Services
{
    public class ApplicationServiceTests
    {
        private readonly Mock<IApplicationRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ApplicationService>> _loggerMock;
        private readonly Mock<NotificationService> _notificationMock; // if this fails, see comment below
        private readonly ApplicationService _sut;

        public ApplicationServiceTests()
        {
            // Create real mocks for dependencies
            var repoMock = new Mock<IApplicationRepository>();
            var mapperMock = new Mock<IMapper>();
            var loggerMock = new Mock<ILogger<ApplicationService>>();
            var notifMock = new Mock<NotificationService>(MockBehavior.Loose, null!, null!, null!, null!);

            // Now pass them into a properly constructed ApplicationService mock
            _serviceMock = new Mock<ApplicationService>(
                repoMock.Object,
                mapperMock.Object,
                loggerMock.Object,
                notifMock.Object
            );

            // Continue as before
            _sut = new ApplicationController(_serviceMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedDtos()
        {
            var entities = new List<Application> { new Application { ApplicationId = 1 }, new Application { ApplicationId = 2 } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 1 }, new ApplicationDto { ApplicationId = 2 } };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(entities)).Returns(dtos);

            var res = await _sut.GetAllAsync();

            res.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetByIdAsync_Existing_ReturnsDto()
        {
            var id = 5;
            var entity = new Application { ApplicationId = id };
            var dto = new ApplicationDto { ApplicationId = id };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<ApplicationDto>(entity)).Returns(dto);

            var res = await _sut.GetByIdAsync(id);

            res.Should().NotBeNull();
            res.ApplicationId.Should().Be(id);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Application?)null);

            Func<Task> act = async () => await _sut.GetByIdAsync(999);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Application with id '999' not found.");
        }

        [Fact]
        public async Task GetByJobAsync_ReturnsMappedDtos()
        {
            var jobId = 11;
            var entities = new List<Application> { new Application { ApplicationId = 1, JobId = jobId } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 1, JobId = jobId } };

            _repoMock.Setup(r => r.GetByJobAsync(jobId)).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(entities)).Returns(dtos);

            var res = await _sut.GetByJobAsync(jobId);

            res.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetByJobSeekerAsync_ReturnsMappedDtos()
        {
            var jsId = Guid.NewGuid();
            var entities = new List<Application> { new Application { ApplicationId = 3, JobSeekerId = jsId } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 3, JobSeekerId = jsId } };

            _repoMock.Setup(r => r.GetByJobSeekerAsync(jsId)).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(entities)).Returns(dtos);

            var res = await _sut.GetByJobSeekerAsync(jsId);

            res.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetShortlistedByJobAsync_ReturnsMappedDtos()
        {
            var jobId = 22;
            var entities = new List<Application> { new Application { ApplicationId = 7, JobId = jobId, IsShortlisted = true } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 7, JobId = jobId, Status = "Shortlisted" } };

            _repoMock.Setup(r => r.GetShortlistedByJobAsync(jobId)).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(entities)).Returns(dtos);

            var res = await _sut.GetShortlistedByJobAsync(jobId);

            res.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetWithInterviewAsync_ReturnsMappedDtos()
        {
            var jobId = 33;
            var entities = new List<Application> { new Application { ApplicationId = 8, JobId = jobId, InterviewDate = DateTime.UtcNow } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 8, JobId = jobId } };

            _repoMock.Setup(r => r.GetWithInterviewAsync(jobId)).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(entities)).Returns(dtos);

            var res = await _sut.GetWithInterviewAsync(jobId);

            res.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task CreateAsync_Valid_CreatesAndReturnsDto_AndCallsNotification()
        {
            var createDto = new CreateApplicationDto { JobId = 10, JobSeekerId = Guid.NewGuid() };
            var entity = new Application { ApplicationId = 100, JobId = 10, JobSeekerId = createDto.JobSeekerId };
            var createdDto = new ApplicationDto { ApplicationId = 100, JobId = 10 };

            _mapperMock.Setup(m => m.Map<Application>(createDto)).Returns(entity);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Application>())).ReturnsAsync(entity);
            // TryGetAppWithDetailsAsync will call GetByIdAsync; ensure repository returns entity
            _repoMock.Setup(r => r.GetByIdAsync(entity.ApplicationId)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<ApplicationDto>(entity)).Returns(createdDto);

            // Notification service should be invoked — return a NotificationDto
            _notificationMock.Setup(n => n.CreateAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto { NotificationId = 1, UserId = Guid.NewGuid(), Message = "ok", CreatedAt = DateTimeOffset.UtcNow });

            var res = await _sut.CreateAsync(createDto);

            res.Should().NotBeNull();
            res.ApplicationId.Should().Be(100);
            _notificationMock.Verify(n => n.CreateAsync(It.IsAny<CreateNotificationDto>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Application?)null);
            var dto = new UpdateApplicationDto { Status = "X" };

            Func<Task> act = async () => await _sut.UpdateAsync(999, dto);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Application with id '999' not found.");
        }

        [Fact]
        public async Task UpdateAsync_Existing_StatusChanged_CreatesNotificationAndReturnsUpdated()
        {
            var id = 55;
            var existing = new Application
            {
                ApplicationId = id,
                Status = "Applied",
                JobSeeker = new JobSeeker { User = new User { UserId = Guid.NewGuid(), Email = "a@b.com", FullName = "Candidate" } },
                Job = new Job { Title = "JobT", Employer = new Employer { CompanyName = "C", User = new User { FullName = "Employer" } } }
            };

            var dto = new UpdateApplicationDto { Status = "Shortlisted" };

            var updated = new Application
            {
                ApplicationId = id,
                Status = "Shortlisted",
                JobSeeker = existing.JobSeeker,
                Job = existing.Job
            };

            var updatedDto = new ApplicationDto { ApplicationId = id, Status = "Shortlisted" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            // mapper.Map(dto, existing) -> we'll simulate by using Callback to change status
            _mapperMock.Setup(m => m.Map(dto, existing)).Callback<UpdateApplicationDto, Application>((d, e) => e.Status = d.Status);
            _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(updated);
            _repoMock.Setup(r => r.GetByIdAsync(updated.ApplicationId)).ReturnsAsync(updated);
            _mapperMock.Setup(m => m.Map<ApplicationDto>(updated)).Returns(updatedDto);

            _notificationMock.Setup(n => n.CreateAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto { NotificationId = 2, UserId = existing.JobSeeker.User.UserId, Message = "shortlisted", CreatedAt = DateTimeOffset.UtcNow });

            var res = await _sut.UpdateAsync(id, dto);

            res.Should().NotBeNull();
            res.Status.Should().Be("Shortlisted");
            _notificationMock.Verify(n => n.CreateAsync(It.IsAny<CreateNotificationDto>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task DeleteAsync_RepoReturnsFalse_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.DeleteAsync(77)).ReturnsAsync(false);

            Func<Task> act = async () => await _sut.DeleteAsync(77);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Application with id '77' not found.");
        }

        [Fact]
        public async Task DeleteAsync_RepoReturnsTrue_ReturnsTrue()
        {
            _repoMock.Setup(r => r.DeleteAsync(10)).ReturnsAsync(true);

            var res = await _sut.DeleteAsync(10);

            res.Should().BeTrue();
        }

        [Fact]
        public async Task MarkReviewedAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.MarkReviewedAsync(123, null)).ReturnsAsync((Application?)null);

            Func<Task> act = async () => await _sut.MarkReviewedAsync(123, null);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Application with id '123' not found.");
        }

        [Fact]
        public async Task MarkReviewedAsync_Existing_ReturnsMappedDto()
        {
            var app = new Application { ApplicationId = 45, ReviewedAt = DateTime.UtcNow, Notes = "ok" };
            var dto = new ApplicationDto { ApplicationId = 45 };

            _repoMock.Setup(r => r.MarkReviewedAsync(45, "note")).ReturnsAsync(app);
            _mapperMock.Setup(m => m.Map<ApplicationDto>(app)).Returns(dto);

            var res = await _sut.MarkReviewedAsync(45, "note");

            res.Should().NotBeNull();
            res.ApplicationId.Should().Be(45);
        }
    }
}
