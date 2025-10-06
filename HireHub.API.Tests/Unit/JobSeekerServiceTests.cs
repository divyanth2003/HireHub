using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoMapper;
using FluentAssertions;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using HireHub.API.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HireHub.API.Tests.Unit
{
    public class JobSeekerServiceTests
    {
        private readonly Mock<IJobSeekerRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<JobSeekerService>> _loggerMock;
        private readonly JobSeekerService _sut;

        public JobSeekerServiceTests()
        {
            _repoMock = new Mock<IJobSeekerRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<JobSeekerService>>();

            _sut = new JobSeekerService(_repoMock.Object, _mapperMock.Object, _loggerMock.Object);
        }

        private JobSeeker MakeJobSeeker(Guid id, Guid userId)
        {
            return new JobSeeker
            {
                JobSeekerId = id,
                UserId = userId,
                College = "TestCollege",
                Skills = "C#;SQL",
                EducationDetails = "BS"
            };
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnMappedDto_WhenCreated()
        {
            // arrange
            var userId = Guid.NewGuid();
            var dto = new CreateJobSeekerDto
            {
                UserId = userId,
                College = "TestCollege",
                Skills = "C#,SQL"
            };

            var createdEntity = MakeJobSeeker(Guid.NewGuid(), userId);

            // setup: repository says no existing jobseeker
            _repoMock.Setup(r => r.ExistsForUserAsync(dto.UserId)).ReturnsAsync(false);

            // setup: mapping dto -> entity
            _mapperMock.Setup(m => m.Map<JobSeeker>(dto)).Returns(new JobSeeker { UserId = dto.UserId, College = dto.College, Skills = dto.Skills });

            // when repository AddAsync called, return createdEntity (with Id)
            _repoMock.Setup(r => r.AddAsync(It.IsAny<JobSeeker>())).ReturnsAsync(createdEntity);

            // mapping returned entity -> dto
            _mapperMock.Setup(m => m.Map<JobSeekerDto>(createdEntity)).Returns(new JobSeekerDto
            {
                JobSeekerId = createdEntity.JobSeekerId,
                UserId = createdEntity.UserId,
                College = createdEntity.College,
                Skills = createdEntity.Skills
            });

            // act
            var result = await _sut.CreateAsync(dto);

            // assert
            result.Should().NotBeNull();
            result.JobSeekerId.Should().Be(createdEntity.JobSeekerId);
            result.UserId.Should().Be(userId);
            result.College.Should().Be(dto.College);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowDuplicate_WhenExists()
        {
            // arrange
            var userId = Guid.NewGuid();
            var dto = new CreateJobSeekerDto { UserId = userId };

            _repoMock.Setup(r => r.ExistsForUserAsync(userId)).ReturnsAsync(true);

            // act
            Func<Task> act = async () => await _sut.CreateAsync(dto);

            // assert
            await act.Should().ThrowAsync<DuplicateEmailException>();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFound_WhenMissing()
        {
            // arrange
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((JobSeeker?)null);

            // act
            Func<Task> act = async () => await _sut.GetByIdAsync(id);

            // assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
