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

            _sut = new ResumeService(
                _repoMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenResumeNotFound()
        {
            // Arrange
            var id = 123;
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Resume?)null);

            // Act
            var act = async () => await _sut.GetByIdAsync(id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Resume with id '{id}' not found.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnMappedDto_WhenCreated()
        {
            // Arrange
            var createDto = new CreateResumeDto
            {
                JobSeekerId = Guid.NewGuid(),
                FilePath = "path/to.pdf",
                ParsedSkills = "C#,SQL"
            };

            var entity = new Resume
            {
                ResumeId = 7,
                JobSeekerId = createDto.JobSeekerId,
                FilePath = createDto.FilePath,
                ParsedSkills = createDto.ParsedSkills,
                UpdatedAt = DateTime.UtcNow
            };

            var returnedDto = new ResumeDto
            {
                ResumeId = entity.ResumeId,
                JobSeekerId = entity.JobSeekerId,
                FilePath = entity.FilePath,
                ParsedSkills = entity.ParsedSkills,
                UpdatedAt = entity.UpdatedAt
            };

            _mapperMock.Setup(m => m.Map<Resume>(createDto)).Returns(entity);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Resume>())).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<ResumeDto>(entity)).Returns(returnedDto);

            // Act
            var result = await _sut.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.ResumeId.Should().Be(entity.ResumeId);
            result.FilePath.Should().Be(createDto.FilePath);
            result.ParsedSkills.Should().Be(createDto.ParsedSkills);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenResumeNotFound()
        {
            // Arrange
            var id = 99;
            var updateDto = new UpdateResumeDto { ParsedSkills = "Updated" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Resume?)null);

            // Act
            var act = async () => await _sut.UpdateAsync(id, updateDto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Resume with id '{id}' not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenDeleteReturnsFalse()
        {
            // Arrange
            var id = 42;
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            // Act
            var act = async () => await _sut.DeleteAsync(id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Resume with id '{id}' not found.");
        }

    
        [Fact]
        public async Task UpdateAsync_ShouldReturnMappedDto_WhenUpdated()
        {
            // Arrange
            var id = 5;
            var existing = new Resume
            {
                ResumeId = id,
                JobSeekerId = Guid.NewGuid(),
                FilePath = "old.pdf",
                ParsedSkills = "Old"
            };

            var updateDto = new UpdateResumeDto
            {
                FilePath = "new.pdf",
                ParsedSkills = "C#,EF"
            };

            var updatedEntity = new Resume
            {
                ResumeId = id,
                JobSeekerId = existing.JobSeekerId,
                FilePath = updateDto.FilePath,
                ParsedSkills = updateDto.ParsedSkills,
                UpdatedAt = DateTime.UtcNow
            };

            var returnedDto = new ResumeDto
            {
                ResumeId = updatedEntity.ResumeId,
                JobSeekerId = updatedEntity.JobSeekerId,
                FilePath = updatedEntity.FilePath,
                ParsedSkills = updatedEntity.ParsedSkills,
                UpdatedAt = updatedEntity.UpdatedAt
            };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _mapperMock.Setup(m => m.Map(updateDto, existing)).Callback(() =>
            {
                // simulate mapping onto the tracked entity
                existing.FilePath = updateDto.FilePath;
                existing.ParsedSkills = updateDto.ParsedSkills;
            });
            _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(updatedEntity);
            _mapperMock.Setup(m => m.Map<ResumeDto>(updatedEntity)).Returns(returnedDto);

            // Act
            var result = await _sut.UpdateAsync(id, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.ResumeId.Should().Be(id);
            result.FilePath.Should().Be(updateDto.FilePath);
            result.ParsedSkills.Should().Be(updateDto.ParsedSkills);
        }
    }
}