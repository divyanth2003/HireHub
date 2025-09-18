//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using AutoMapper;
//using FluentAssertions;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit;
//using HireHub.API.DTOs;
//using HireHub.API.Models;
//using HireHub.API.Repositories.Interfaces;
//using HireHub.API.Services;
//using HireHub.API.Exceptions;

//namespace HireHub.API.Tests.Unit
//{
//    public class ApplicationServiceTests
//    {
//        private readonly Mock<IApplicationRepository> _repoMock;
//        private readonly Mock<IMapper> _mapperMock;
//        private readonly Mock<ILogger<ApplicationService>> _loggerMock;
//        private readonly ApplicationService _sut;

//        public ApplicationServiceTests()
//        {
//            _repoMock = new Mock<IApplicationRepository>();
//            _mapperMock = new Mock<IMapper>();
//            _loggerMock = new Mock<ILogger<ApplicationService>>();

//            _sut = new ApplicationService(
//                _repoMock.Object,
//                _mapperMock.Object,
//                _loggerMock.Object
//            );
//        }

//        [Fact]
//        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenNotFound()
//        {
//            // Arrange
//            var id = 11;
//            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Application?)null);

//            // Act
//            var act = async () => await _sut.GetByIdAsync(id);

//            // Assert
//            await act.Should().ThrowAsync<NotFoundException>()
//                .WithMessage($"Application with id '{id}' not found.");
//        }

//        [Fact]
//        public async Task CreateAsync_ShouldReturnMappedDto_WhenCreated()
//        {
//            // Arrange
//            var createDto = new CreateApplicationDto
//            {
//                JobId = 2,
//                JobSeekerId = Guid.NewGuid(),
//                ResumeId = 3,
//                CoverLetter = "cover"
//            };

//            var entity = new Application
//            {
//                ApplicationId = 7,
//                JobId = createDto.JobId,
//                JobSeekerId = createDto.JobSeekerId,
//                ResumeId = createDto.ResumeId,
//                CoverLetter = createDto.CoverLetter,
//                Status = "Applied",
//                AppliedAt = DateTime.UtcNow
//            };

//            var persisted = new Application
//            {
//                ApplicationId = entity.ApplicationId,
//                JobId = entity.JobId,
//                JobSeekerId = entity.JobSeekerId,
//                ResumeId = entity.ResumeId,
//                CoverLetter = entity.CoverLetter,
//                Status = entity.Status,
//                AppliedAt = entity.AppliedAt
//            };

//            var returnedDto = new ApplicationDto
//            {
//                ApplicationId = persisted.ApplicationId,
//                JobId = persisted.JobId,
//                JobSeekerId = persisted.JobSeekerId,
//                ResumeId = persisted.ResumeId,
//                CoverLetter = persisted.CoverLetter,
//                Status = persisted.Status
//            };

//            _mapperMock.Setup(m => m.Map<Application>(createDto)).Returns(entity);
//            _repoMock.Setup(r => r.AddAsync(It.IsAny<Application>())).ReturnsAsync(persisted);
//            _repoMock.Setup(r => r.GetByIdAsync(persisted.ApplicationId)).ReturnsAsync(persisted);
//            _mapperMock.Setup(m => m.Map<ApplicationDto>(persisted)).Returns(returnedDto);

//            // Act
//            var result = await _sut.CreateAsync(createDto);

//            // Assert
//            result.Should().NotBeNull();
//            result.ApplicationId.Should().Be(persisted.ApplicationId);
//            result.JobId.Should().Be(createDto.JobId);
//            result.JobSeekerId.Should().Be(createDto.JobSeekerId);
//        }

//        [Fact]
//        public async Task GetByJobAsync_ShouldReturnMappedList()
//        {
//            // Arrange
//            var jobId = 5;
//            var list = new List<Application>
//            {
//                new Application { ApplicationId = 1, JobId = jobId, JobSeekerId = Guid.NewGuid() },
//                new Application { ApplicationId = 2, JobId = jobId, JobSeekerId = Guid.NewGuid() }
//            };

//            var dtoList = new List<ApplicationDto>
//            {
//                new ApplicationDto { ApplicationId = 1, JobId = jobId },
//                new ApplicationDto { ApplicationId = 2, JobId = jobId }
//            };

//            _repoMock.Setup(r => r.GetByJobAsync(jobId)).ReturnsAsync(list);
//            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(list)).Returns(dtoList);

//            // Act
//            var result = await _sut.GetByJobAsync(jobId);

//            // Assert
//            result.Should().NotBeNull();
//            result.Should().HaveCount(2);
//        }

//        [Fact]
//        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenNotExists()
//        {
//            // Arrange
//            var id = 99;
//            var updateDto = new UpdateApplicationDto { CoverLetter = "x" };
//            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Application?)null);

//            // Act
//            var act = async () => await _sut.UpdateAsync(id, updateDto);

//            // Assert
//            await act.Should().ThrowAsync<NotFoundException>()
//                .WithMessage($"Application with id '{id}' not found.");
//        }

//        [Fact]
//        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenDeleteReturnsFalse()
//        {
//            // Arrange
//            var id = 42;
//            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

//            // Act
//            var act = async () => await _sut.DeleteAsync(id);

//            // Assert
//            await act.Should().ThrowAsync<NotFoundException>()
//                .WithMessage($"Application with id '{id}' not found.");
//        }

   

//    }
//}