//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using AutoMapper;
//using FluentAssertions;
//using HireHub.API.DTOs;
//using HireHub.API.Models;
//using HireHub.API.Repositories.Interfaces;
//using HireHub.API.Services;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit;

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

//            _sut = new ApplicationService(_repoMock.Object, _mapperMock.Object, _loggerMock.Object);
//        }

//        [Fact]
//        public async Task GetByIdAsync_ShouldThrowNotFound_WhenMissing()
//        {
//            // arrange
//            var id = 100;
//            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Application?)null);

//            // act
//            Func<Task> act = async () => await _sut.GetByIdAsync(id);

//            // assert
//            await act.Should().ThrowAsync<Exception>().Where(e => e.Message.Contains("not found"));
//        }

//        [Fact]
//        public async Task CreateAsync_ShouldReturnMappedDto_WhenCreated()
//        {
//            // arrange
//            var dto = new CreateApplicationDto
//            {
//                JobId = 1,
//                JobSeekerId = Guid.NewGuid(),
//                ResumeId = null,
//                CoverLetter = "Hi"
//            };

//            var entity = new Application
//            {
//                ApplicationId = 55,
//                JobId = dto.JobId,
//                JobSeekerId = dto.JobSeekerId,
//                ResumeId = dto.ResumeId ?? 0,
//                CoverLetter = dto.CoverLetter,
//                Status = "Applied",
//                AppliedAt = DateTime.UtcNow
//            };

//            var mappedDto = new ApplicationDto
//            {
//                ApplicationId = entity.ApplicationId,
//                JobId = entity.JobId,
//                JobSeekerId = entity.JobSeekerId,
//                CoverLetter = entity.CoverLetter,
//                Status = entity.Status,
//                AppliedAt = entity.AppliedAt
//            };

//            _mapperMock.Setup(m => m.Map<Application>(dto)).Returns(entity);
//            _repoMock.Setup(r => r.AddAsync(It.IsAny<Application>())).ReturnsAsync(entity);
//            _repoMock.Setup(r => r.GetByIdAsync(entity.ApplicationId)).ReturnsAsync(entity);
//            _mapperMock.Setup(m => m.Map<ApplicationDto>(entity)).Returns(mappedDto);

//            // act
//            var result = await _sut.CreateAsync(dto);

//            // assert
//            result.Should().NotBeNull();
//            result.ApplicationId.Should().Be(entity.ApplicationId);
//            _repoMock.Verify(r => r.AddAsync(It.IsAny<Application>()), Times.Once);
//        }

//        [Fact]
//        public async Task UpdateAsync_ShouldThrowNotFound_WhenMissing()
//        {
//            // arrange
//            var id = 77;
//            var updateDto = new UpdateApplicationDto { Status = "Reviewed" };
//            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Application?)null);

//            Func<Task> act = async () => await _sut.UpdateAsync(id, updateDto);

//            await act.Should().ThrowAsync<Exception>().Where(e => e.Message.Contains("not found"));
//        }

//        [Fact]
//        public async Task MarkReviewedAsync_ShouldReturnMappedDto_WhenUpdated()
//        {
//            // arrange
//            var appId = 200;
//            var notes = "Looks good";

//            var updatedEntity = new Application
//            {
//                ApplicationId = appId,
//                JobId = 1,
//                JobSeekerId = Guid.NewGuid(),
//                ReviewedAt = DateTime.UtcNow,
//                Notes = notes
//            };

//            var mappedDto = new ApplicationDto
//            {
//                ApplicationId = appId,
//                JobId = updatedEntity.JobId,
//                JobSeekerId = updatedEntity.JobSeekerId,
//                ReviewedAt = updatedEntity.ReviewedAt,
//                Notes = updatedEntity.Notes
//            };

//            _repoMock.Setup(r => r.MarkReviewedAsync(appId, notes)).ReturnsAsync(updatedEntity);
//            _mapperMock.Setup(m => m.Map<ApplicationDto>(updatedEntity)).Returns(mappedDto);

//            // act
//            var result = await _sut.MarkReviewedAsync(appId, notes);

//            // assert
//            result.Should().NotBeNull();
//            result!.ApplicationId.Should().Be(appId);
//            result.Notes.Should().Be(notes);
//        }

//        [Fact]
//        public async Task GetByJobAsync_ReturnsMappedList()
//        {
//            // arrange
//            var jobId = 5;
//            var list = new List<Application>
//            {
//                new Application { ApplicationId = 1, JobId = jobId, JobSeekerId = Guid.NewGuid() }
//            };

//            var dtoList = new List<ApplicationDto>
//            {
//                new ApplicationDto { ApplicationId = 1, JobId = jobId }
//            };

//            _repoMock.Setup(r => r.GetByJobAsync(jobId)).ReturnsAsync(list);
//            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(list)).Returns(dtoList);

//            // act
//            var result = await _sut.GetByJobAsync(jobId);

//            // assert
//            result.Should().NotBeNull();
//            result.Should().HaveCount(1);
//        }
//    }
//}
