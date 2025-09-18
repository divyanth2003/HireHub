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
    public class JobSeekerServiceTests
    {
        private readonly Mock<IJobSeekerRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<JobSeekerService>> _loggerMock;
        private readonly JobSeekerService _sut; // System Under Test

        public JobSeekerServiceTests()
        {
            _repoMock = new Mock<IJobSeekerRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<JobSeekerService>>();

            _sut = new JobSeekerService(
                _repoMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenJobSeekerDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync((JobSeeker?)null);

            // Act
            var act = async () => await _sut.GetByIdAsync(id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"JobSeeker with id '{id}' not found.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnMappedDto_WhenCreated()
        {
            // Arrange
            var createDto = new CreateJobSeekerDto
            {
                UserId = Guid.NewGuid(),
                EducationDetails = "BSc Computer Science",
                Skills = "C#, SQL, ASP.NET",
               
                College = "XYZ University",
                WorkStatus = "Employed",
                Experience = "2"
            };

            var mappedEntity = new JobSeeker
            {
                UserId = createDto.UserId,
                EducationDetails = createDto.EducationDetails,
                Skills = createDto.Skills,
               
                College = createDto.College,
                WorkStatus = createDto.WorkStatus,
                Experience = createDto.Experience
            };

            _mapperMock.Setup(m => m.Map<JobSeeker>(createDto)).Returns(mappedEntity);

            _repoMock.Setup(r => r.AddAsync(It.IsAny<JobSeeker>()))
                     .ReturnsAsync((JobSeeker j) =>
                     {
                         j.JobSeekerId = Guid.NewGuid(); // simulate DB assigning ID
                         return j;
                     });

            _mapperMock.Setup(m => m.Map<JobSeekerDto>(It.IsAny<JobSeeker>()))
                       .Returns((JobSeeker j) => new JobSeekerDto
                       {
                           JobSeekerId = j.JobSeekerId,
                           UserId = j.UserId,
                           UserFullName = string.Empty,
                           UserEmail = string.Empty,
                           EducationDetails = j.EducationDetails,
                           Skills = j.Skills,
                           
                           College = j.College,
                           WorkStatus = j.WorkStatus,
                           Experience = j.Experience
                       });

            // Act
            var result = await _sut.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(createDto.UserId);
            
            result.College.Should().Be("XYZ University");
            result.Experience.Should().Be("2");
            result.JobSeekerId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenJobSeekerDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new UpdateJobSeekerDto
            {
                EducationDetails = "Updated MSc",
                
            };

            _repoMock.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync((JobSeeker?)null);

            // Act
            var act = async () => await _sut.UpdateAsync(id, updateDto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"JobSeeker with id '{id}' not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenJobSeekerDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.DeleteAsync(id))
                     .ReturnsAsync(false);

            // Act
            var act = async () => await _sut.DeleteAsync(id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"JobSeeker with id '{id}' not found.");
        }
    }
}
