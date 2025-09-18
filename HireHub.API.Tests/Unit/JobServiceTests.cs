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

namespace HireHub.API.Tests.Unit
{
    public class JobServiceTests
    {
        private readonly Mock<IJobRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<JobService>> _loggerMock;
        private readonly JobService _sut;

        public JobServiceTests()
        {
            _repoMock = new Mock<IJobRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<JobService>>();

            _sut = new JobService(
                _repoMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos()
        {
            // Arrange
            var entities = new List<Job>
            {
                new Job { JobId = 1, Title = "A" },
                new Job { JobId = 2, Title = "B" }
            };
            var dtos = new List<JobDto>
            {
                new JobDto { JobId = 1, Title = "A" },
                new JobDto { JobId = 2, Title = "B" }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(entities)).Returns(dtos);

            // Act
            var result = await _sut.GetAllAsync();

            // Assert
            result.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenNotExists()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(123)).ReturnsAsync((Job?)null);

            // Act
            var act = async () => await _sut.GetByIdAsync(123);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Job with id '123' not found.");
        }

        [Fact]
        public async Task GetByEmployerAsync_ShouldReturnMappedDtos()
        {
            // Arrange
            var employerId = Guid.NewGuid();
            var entities = new List<Job>
            {
                new Job { JobId = 1, EmployerId = employerId, Title = "X" }
            };
            var dtos = new List<JobDto> { new JobDto { JobId = 1, EmployerId = employerId, Title = "X" } };

            _repoMock.Setup(r => r.GetByEmployerAsync(employerId)).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(entities)).Returns(dtos);

            // Act
            var result = await _sut.GetByEmployerAsync(employerId);

            // Assert
            result.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreatedDto()
        {
            // Arrange
            var createDto = new CreateJobDto { Title = "New", EmployerId = Guid.NewGuid() };
            var jobEntity = new Job { JobId = 11, Title = "New", EmployerId = createDto.EmployerId };
            var jobDto = new JobDto { JobId = 11, Title = "New", EmployerId = createDto.EmployerId };

            _mapperMock.Setup(m => m.Map<Job>(createDto)).Returns(jobEntity);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Job>())).ReturnsAsync(jobEntity);
            _mapperMock.Setup(m => m.Map<JobDto>(jobEntity)).Returns(jobDto);

            // Act
            var result = await _sut.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.JobId.Should().Be(11);
            result.Title.Should().Be("New");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenNotExists()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Job?)null);
            var dto = new UpdateJobDto { Title = "Updated" };

            // Act
            var act = async () => await _sut.UpdateAsync(99, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Job with id '99' not found.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnUpdatedDto_WhenExists()
        {
            // Arrange
            var id = 5;
            var existing = new Job { JobId = id, Title = "Old" };
            var dto = new UpdateJobDto { Title = "Updated" };
            var updatedEntity = new Job { JobId = id, Title = "Updated" };
            var updatedDto = new JobDto { JobId = id, Title = "Updated" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _mapperMock.Setup(m => m.Map(dto, existing)).Callback<UpdateJobDto, Job>((d, e) => e.Title = d.Title);
            _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(updatedEntity);
            _mapperMock.Setup(m => m.Map<JobDto>(updatedEntity)).Returns(updatedDto);

            // Act
            var result = await _sut.UpdateAsync(id, dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated");
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenRepositoryReturnsFalse()
        {
            // Arrange
            _repoMock.Setup(r => r.DeleteAsync(77)).ReturnsAsync(false);

            // Act
            var act = async () => await _sut.DeleteAsync(77);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Job with id '77' not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenRepositoryDeletes()
        {
            // Arrange
            _repoMock.Setup(r => r.DeleteAsync(10)).ReturnsAsync(true);

            // Act
            var result = await _sut.DeleteAsync(10);

            // Assert
            result.Should().BeTrue();
        }
    }
}