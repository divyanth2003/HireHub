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
    public class ResumeServiceTests
    {
        private readonly Mock<IResumeRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ResumeService>> _loggerMock;
        private readonly ResumeService _sut;

        public ResumeServiceTests()
        {
            _repoMock = new Mock<IResumeRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ResumeService>>();

            _sut = new ResumeService(_repoMock.Object, _mapperMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnMappedDto_WhenCreated()
        {
            // arrange
            var jobSeekerId = Guid.NewGuid();
            var createDto = new CreateResumeDto
            {
                JobSeekerId = jobSeekerId,
                ResumeName = "CV1",
                FilePath = "/files/cv1.pdf",
                FileType = "pdf",
                ParsedSkills = "C#"
            };

            var mappedEntity = new Resume
            {
                ResumeId = 123,
                JobSeekerId = jobSeekerId,
                ResumeName = createDto.ResumeName,
                FilePath = createDto.FilePath,
                FileType = createDto.FileType,
                ParsedSkills = createDto.ParsedSkills,
                IsDefault = false,
                UpdatedAt = DateTime.UtcNow
            };

            var mappedDto = new ResumeDto
            {
                ResumeId = mappedEntity.ResumeId,
                JobSeekerId = jobSeekerId,
                ResumeName = mappedEntity.ResumeName,
                FilePath = mappedEntity.FilePath
            };

            // repository: no duplicate
            _repoMock.Setup(r => r.ExistsByNameAsync(jobSeekerId, createDto.ResumeName)).ReturnsAsync(false);

            // mapper: CreateDto -> Resume
            _mapperMock.Setup(m => m.Map<Resume>(createDto)).Returns(mappedEntity);

            // repo add returns the entity (with id)
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Resume>())).ReturnsAsync(mappedEntity);

            // mapper: Resume -> ResumeDto
            _mapperMock.Setup(m => m.Map<ResumeDto>(mappedEntity)).Returns(mappedDto);

            // act
            var result = await _sut.CreateAsync(createDto);

            // assert
            result.Should().NotBeNull();
            result.ResumeId.Should().Be(mappedEntity.ResumeId);

            _repoMock.Verify(r => r.ExistsByNameAsync(jobSeekerId, createDto.ResumeName), Times.Once);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Resume>()), Times.Once);
        }

        [Fact]
        public async Task SetDefaultAsync_ShouldThrow_WhenResumeNotFound()
        {
            // arrange
            var jobSeekerId = Guid.NewGuid();
            var resumeId = 999;

            _repoMock.Setup(r => r.GetByIdAsync(resumeId)).ReturnsAsync((Resume?)null);

            // act
            Func<Task> act = async () => await _sut.SetDefaultAsync(jobSeekerId, resumeId);

            // assert
            await act.Should().ThrowAsync<Exception>()
                .Where(e => e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetDefaultByJobSeekerAsync_ReturnsNull_WhenNoDefault()
        {
            // arrange
            var jobSeekerId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetDefaultByJobSeekerAsync(jobSeekerId)).ReturnsAsync((Resume?)null);

            // act
            var result = await _sut.GetDefaultByJobSeekerAsync(jobSeekerId);

            // assert
            result.Should().BeNull();
        }
    }
}
