using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HireHub.API.Tests.Services
{
    public class EmployerServiceTests
    {
        private readonly Mock<IEmployerRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<EmployerService>> _loggerMock;
        private readonly EmployerService _service;

        public EmployerServiceTests()
        {
            _repoMock = new Mock<IEmployerRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<EmployerService>>();

            _service = new EmployerService(
                _repoMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_WhenCalled_ReturnsMappedDtos()
        {
            // Arrange
            var employers = new List<Employer> { new Employer { EmployerId = Guid.NewGuid(), CompanyName = "A" } };
            var dtos = new List<EmployerDto> { new EmployerDto { EmployerId = employers[0].EmployerId, CompanyName = "A" } };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(employers);
            _mapperMock.Setup(m => m.Map<IEnumerable<EmployerDto>>(employers)).Returns(dtos);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsMappedDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var employer = new Employer { EmployerId = id, CompanyName = "X" };
            var dto = new EmployerDto { EmployerId = id, CompanyName = "X" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(employer);
            _mapperMock.Setup(m => m.Map<EmployerDto>(employer)).Returns(dto);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.CompanyName, result.CompanyName);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Employer?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));
        }

        [Fact]
        public async Task GetByUserIdAsync_ExistingUser_ReturnsMappedDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var employer = new Employer { EmployerId = Guid.NewGuid(), UserId = userId, CompanyName = "Co" };
            var dto = new EmployerDto { EmployerId = employer.EmployerId, CompanyName = "Co" };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(employer);
            _mapperMock.Setup(m => m.Map<EmployerDto>(employer)).Returns(dto);

            // Act
            var result = await _service.GetByUserIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Co", result.CompanyName);
        }

        [Fact]
        public async Task GetByUserIdAsync_NotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Employer?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByUserIdAsync(userId));
        }

        [Fact]
        public async Task SearchByCompanyNameAsync_WhenCalled_ReturnsMappedDtos()
        {
            var company = "tech";
            var employers = new List<Employer> { new Employer { EmployerId = Guid.NewGuid(), CompanyName = "TechCo" } };
            var dtos = new List<EmployerDto> { new EmployerDto { EmployerId = employers[0].EmployerId, CompanyName = "TechCo" } };

            _repoMock.Setup(r => r.SearchByCompanyNameAsync(company)).ReturnsAsync(employers);
            _mapperMock.Setup(m => m.Map<IEnumerable<EmployerDto>>(employers)).Returns(dtos);

            var result = await _service.SearchByCompanyNameAsync(company);

            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetByJobIdAsync_ExistingJob_ReturnsMappedDto()
        {
            var jobId = 5;
            var employer = new Employer { EmployerId = Guid.NewGuid(), CompanyName = "JobCo" };
            var dto = new EmployerDto { EmployerId = employer.EmployerId, CompanyName = "JobCo" };

            _repoMock.Setup(r => r.GetByJobIdAsync(jobId)).ReturnsAsync(employer);
            _mapperMock.Setup(m => m.Map<EmployerDto>(employer)).Returns(dto);

            var result = await _service.GetByJobIdAsync(jobId);

            Assert.NotNull(result);
            Assert.Equal("JobCo", result.CompanyName);
        }

        [Fact]
        public async Task GetByJobIdAsync_NotFound_ThrowsNotFoundException()
        {
            var jobId = 99;
            _repoMock.Setup(r => r.GetByJobIdAsync(jobId)).ReturnsAsync((Employer?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByJobIdAsync(jobId));
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            // Arrange
            var dto = new CreateEmployerDto { CompanyName = "NewCo", UserId = Guid.NewGuid(), ContactInfo = "c" };
            var mapped = new Employer { EmployerId = Guid.NewGuid(), CompanyName = dto.CompanyName, UserId = dto.UserId };
            var created = new Employer { EmployerId = mapped.EmployerId, CompanyName = mapped.CompanyName, UserId = mapped.UserId };
            var returnedDto = new EmployerDto { EmployerId = created.EmployerId, CompanyName = created.CompanyName };

            _mapperMock.Setup(m => m.Map<Employer>(dto)).Returns(mapped);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Employer>())).ReturnsAsync(created);
            _mapperMock.Setup(m => m.Map<EmployerDto>(created)).Returns(returnedDto);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(returnedDto.CompanyName, result.CompanyName);
        }

        [Fact]
        public async Task UpdateAsync_Existing_ReturnsUpdatedDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateEmployerDto { CompanyName = "UpdCo", ContactInfo = "u" };
            var existing = new Employer { EmployerId = id, CompanyName = "Old" };
            var updated = new Employer { EmployerId = id, CompanyName = "UpdCo" };
            var returnedDto = new EmployerDto { EmployerId = id, CompanyName = "UpdCo" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _mapperMock.Setup(m => m.Map(dto, existing));
            _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(updated);
            _mapperMock.Setup(m => m.Map<EmployerDto>(updated)).Returns(returnedDto);

            // Act
            var result = await _service.UpdateAsync(id, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("UpdCo", result.CompanyName);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateEmployerDto { CompanyName = "No" };
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Employer?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task DeleteAsync_Existing_ReturnsTrue()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(id);

            Assert.True(result);
            _repoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
        }
    }
}
