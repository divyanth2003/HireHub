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
    public class EmployerServiceTests
    {
        private readonly Mock<IEmployerRepository> _employerRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<EmployerService>> _loggerMock;
        private readonly EmployerService _sut; // System Under Test

        public EmployerServiceTests()
        {
            _employerRepoMock = new Mock<IEmployerRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<EmployerService>>();

            _sut = new EmployerService(
                _employerRepoMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenEmployerNotFound()
        {
            // Arrange
            var employerId = Guid.NewGuid();
            _employerRepoMock.Setup(r => r.GetByIdAsync(employerId))
                             .ReturnsAsync((Employer?)null);

            // Act
            var act = async () => await _sut.GetByIdAsync(employerId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Employer with id '{employerId}' not found.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnEmployerDto_WhenSuccessful()
        {
            // Arrange
            var dto = new CreateEmployerDto { CompanyName = "TestCo", ContactInfo = "123-456" };
            var employer = new Employer { EmployerId = Guid.NewGuid(), CompanyName = dto.CompanyName, ContactInfo = dto.ContactInfo };
            var employerDto = new EmployerDto { EmployerId = employer.EmployerId, CompanyName = employer.CompanyName, ContactInfo = employer.ContactInfo };

            _mapperMock.Setup(m => m.Map<Employer>(dto)).Returns(employer);
            _employerRepoMock.Setup(r => r.AddAsync(employer)).ReturnsAsync(employer);
            _mapperMock.Setup(m => m.Map<EmployerDto>(employer)).Returns(employerDto);

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.CompanyName.Should().Be(dto.CompanyName);
            result.ContactInfo.Should().Be(dto.ContactInfo);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenEmployerNotFound()
        {
            // Arrange
            var employerId = Guid.NewGuid();
            var dto = new UpdateEmployerDto { CompanyName = "UpdatedCo", ContactInfo = "987-654" };

            _employerRepoMock.Setup(r => r.GetByIdAsync(employerId))
                             .ReturnsAsync((Employer?)null);

            // Act
            var act = async () => await _sut.UpdateAsync(employerId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Employer with id '{employerId}' not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenEmployerNotFound()
        {
            // Arrange
            var employerId = Guid.NewGuid();
            _employerRepoMock.Setup(r => r.DeleteAsync(employerId)).ReturnsAsync(false);

            // Act
            var act = async () => await _sut.DeleteAsync(employerId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Employer with id '{employerId}' not found.");
        }
    }
}