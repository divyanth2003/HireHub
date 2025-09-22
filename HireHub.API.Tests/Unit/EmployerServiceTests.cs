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
        private readonly Mock<IEmployerRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<EmployerService>> _loggerMock;
        private readonly EmployerService _sut;

        public EmployerServiceTests()
        {
            _repoMock = new Mock<IEmployerRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<EmployerService>>();

            _sut = new EmployerService(
                _repoMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedList()
        {
            var entities = new List<Employer>
            {
                new Employer { EmployerId = Guid.NewGuid(), CompanyName = "A" },
                new Employer { EmployerId = Guid.NewGuid(), CompanyName = "B" }
            };
            var dtos = new List<EmployerDto>
            {
                new EmployerDto { EmployerId = entities[0].EmployerId, CompanyName = "A" },
                new EmployerDto { EmployerId = entities[1].EmployerId, CompanyName = "B" }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<EmployerDto>>(entities)).Returns(dtos);

            var result = await _sut.GetAllAsync();

            result.Should().NotBeNull().And.HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenFound()
        {
            var id = Guid.NewGuid();
            var entity = new Employer { EmployerId = id, CompanyName = "C" };
            var dto = new EmployerDto { EmployerId = id, CompanyName = "C" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<EmployerDto>(entity)).Returns(dto);

            var result = await _sut.GetByIdAsync(id);

            result.Should().NotBeNull();
            result.EmployerId.Should().Be(id);
            result.CompanyName.Should().Be("C");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFound_WhenMissing()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Employer?)null);

            var act = async () => await _sut.GetByIdAsync(id);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Employer with id '{id}' not found.");
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnMappedDto_WhenFound()
        {
            var userId = Guid.NewGuid();
            var entity = new Employer { EmployerId = Guid.NewGuid(), CompanyName = "OwnerCo", UserId = userId };
            var dto = new EmployerDto { EmployerId = entity.EmployerId, CompanyName = "OwnerCo" };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<EmployerDto>(entity)).Returns(dto);

            var result = await _sut.GetByUserIdAsync(userId);

            result.Should().NotBeNull();
            result.EmployerId.Should().Be(entity.EmployerId);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldThrowNotFound_WhenMissing()
        {
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Employer?)null);

            var act = async () => await _sut.GetByUserIdAsync(userId);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Employer with user id '{userId}' not found.");
        }

        [Fact]
        public async Task SearchByCompanyNameAsync_ShouldReturnMappedList()
        {
            var list = new List<Employer> { new Employer { EmployerId = Guid.NewGuid(), CompanyName = "X Inc" } };
            var dtos = new List<EmployerDto> { new EmployerDto { EmployerId = list[0].EmployerId, CompanyName = "X Inc" } };

            _repoMock.Setup(r => r.SearchByCompanyNameAsync("X")).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<EmployerDto>>(list)).Returns(dtos);

            var result = await _sut.SearchByCompanyNameAsync("X");

            result.Should().NotBeNull().And.HaveCount(1);
        }

        [Fact]
        public async Task GetByJobIdAsync_ShouldReturnMappedDto_WhenFound()
        {
            var jobId = 42;
            var entity = new Employer { EmployerId = Guid.NewGuid(), CompanyName = "ByJob" };
            var dto = new EmployerDto { EmployerId = entity.EmployerId, CompanyName = "ByJob" };

            _repoMock.Setup(r => r.GetByJobIdAsync(jobId)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<EmployerDto>(entity)).Returns(dto);

            var result = await _sut.GetByJobIdAsync(jobId);

            result.Should().NotBeNull();
            result.CompanyName.Should().Be("ByJob");
        }

        [Fact]
        public async Task GetByJobIdAsync_ShouldThrowNotFound_WhenMissing()
        {
            var jobId = 99;
            _repoMock.Setup(r => r.GetByJobIdAsync(jobId)).ReturnsAsync((Employer?)null);

            var act = async () => await _sut.GetByJobIdAsync(jobId);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Employer for job id '{jobId}' not found.");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowDuplicate_WhenCompanyExists()
        {
            var dto = new CreateEmployerDto { CompanyName = "DupCo", UserId = Guid.NewGuid() };

            _repoMock.Setup(r => r.ExistsByCompanyNameAsync(dto.CompanyName)).ReturnsAsync(true);

            var act = async () => await _sut.CreateAsync(dto);

            await act.Should().ThrowAsync<DuplicateEmailException>() // service used DuplicateEmailException
                .WithMessage($"Company '{dto.CompanyName}' already exists.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnMappedDto_WhenCreated()
        {
            var dtoIn = new CreateEmployerDto { CompanyName = "NewCo", UserId = Guid.NewGuid() };

            var entity = new Employer { CompanyName = dtoIn.CompanyName, UserId = dtoIn.UserId };
            var createdEntity = new Employer { EmployerId = Guid.NewGuid(), CompanyName = dtoIn.CompanyName, UserId = dtoIn.UserId };
            var returnedDto = new EmployerDto { EmployerId = createdEntity.EmployerId, CompanyName = createdEntity.CompanyName };

            _repoMock.Setup(r => r.ExistsByCompanyNameAsync(dtoIn.CompanyName)).ReturnsAsync(false);
            _mapperMock.Setup(m => m.Map<Employer>(dtoIn)).Returns(entity);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Employer>())).ReturnsAsync(createdEntity);
            _mapperMock.Setup(m => m.Map<EmployerDto>(createdEntity)).Returns(returnedDto);

            var result = await _sut.CreateAsync(dtoIn);

            result.Should().NotBeNull();
            result.CompanyName.Should().Be(dtoIn.CompanyName);
            result.EmployerId.Should().NotBeEmpty();   // ✅ only check not empty
        }


        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFound_WhenMissing()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateEmployerDto { CompanyName = "U" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Employer?)null);

            var act = async () => await _sut.UpdateAsync(id, dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Employer with id '{id}' not found.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnMappedDto_WhenUpdated()
        {
            var id = Guid.NewGuid();
            var existing = new Employer { EmployerId = id, CompanyName = "Old" };
            var updatedEntity = new Employer { EmployerId = id, CompanyName = "New" };
            var returnedDto = new EmployerDto { EmployerId = id, CompanyName = "New" };
            var dto = new UpdateEmployerDto { CompanyName = "New" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _mapperMock.Setup(m => m.Map(dto, existing)).Callback<UpdateEmployerDto, Employer>((d, e) => e.CompanyName = d.CompanyName);
            _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(updatedEntity);
            _mapperMock.Setup(m => m.Map<EmployerDto>(updatedEntity)).Returns(returnedDto);

            var result = await _sut.UpdateAsync(id, dto);

            result.Should().NotBeNull();
            result.CompanyName.Should().Be("New");
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFound_WhenDeleteReturnsFalse()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            var act = async () => await _sut.DeleteAsync(id);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Employer with id '{id}' not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenDeleted()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _sut.DeleteAsync(id);

            result.Should().BeTrue();
        }
    }
}
