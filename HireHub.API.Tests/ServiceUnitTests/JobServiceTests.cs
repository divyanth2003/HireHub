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
    public class JobServiceTests
    {
        private readonly Mock<IJobRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<JobService>> _loggerMock;
        private readonly JobService _service;

        public JobServiceTests()
        {
            _repoMock = new Mock<IJobRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<JobService>>();
            _service = new JobService(_repoMock.Object, _mapperMock.Object, _loggerMock.Object);
        }

     
        [Fact]
        public async Task GetAllAsync_WhenCalled_ReturnsMappedDtos()
        {
            var list = new List<Job> { new Job { JobId = 1, Title = "A" } };
            var dtos = new List<JobDto> { new JobDto { JobId = 1, Title = "A" } };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(list)).Returns(dtos);

            var res = await _service.GetAllAsync();
            Assert.Single(res);
        }

        [Fact]
        public async Task GetByIdAsync_Existing_ReturnsDto()
        {
            var job = new Job { JobId = 10, Title = "Dev" };
            var dto = new JobDto { JobId = 10, Title = "Dev" };

            _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(job);
            _mapperMock.Setup(m => m.Map<JobDto>(job)).Returns(dto);

            var result = await _service.GetByIdAsync(10);
            Assert.Equal(10, result.JobId);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_Throws()
        {
            _repoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync((Job?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(100));
        }

        [Fact]
        public async Task GetByEmployerAsync_WhenCalled_ReturnsMappedDtos()
        {
            var empId = Guid.NewGuid();
            var list = new List<Job> { new Job { EmployerId = empId } };
            var dtos = new List<JobDto> { new JobDto { EmployerId = empId } };

            _repoMock.Setup(r => r.GetByEmployerAsync(empId)).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(list)).Returns(dtos);

            var result = await _service.GetByEmployerAsync(empId);
            Assert.Single(result);
        }

        [Fact]
        public async Task SearchByTitleAsync_ReturnsMappedDtos()
        {
            var jobs = new List<Job> { new Job { Title = "Dev" } };
            var dtos = new List<JobDto> { new JobDto { Title = "Dev" } };

            _repoMock.Setup(r => r.SearchByTitleAsync("Dev")).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var result = await _service.SearchByTitleAsync("Dev");
            Assert.Single(result);
        }

        [Fact]
        public async Task SearchByLocationAsync_ReturnsMappedDtos()
        {
            var jobs = new List<Job> { new Job { Location = "Chennai" } };
            var dtos = new List<JobDto> { new JobDto { Location = "Chennai" } };

            _repoMock.Setup(r => r.SearchByLocationAsync("Chennai")).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var result = await _service.SearchByLocationAsync("Chennai");
            Assert.Single(result);
        }

        [Fact]
        public async Task SearchBySkillAsync_ReturnsMappedDtos()
        {
            var jobs = new List<Job> { new Job { SkillsRequired = "C#" } };
            var dtos = new List<JobDto> { new JobDto { SkillsRequired = "C#" } };

            _repoMock.Setup(r => r.SearchBySkillAsync("C#")).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var result = await _service.SearchBySkillAsync("C#");
            Assert.Single(result);
        }

        [Fact]
        public async Task SearchByCompanyAsync_ReturnsMappedDtos()
        {
            var jobs = new List<Job> { new Job { Title = "Intern" } };
            var dtos = new List<JobDto> { new JobDto { Title = "Intern" } };

            _repoMock.Setup(r => r.SearchByCompanyAsync("TechCorp")).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var result = await _service.SearchByCompanyAsync("TechCorp");
            Assert.Single(result);
        }

        // ------------------- CREATE -------------------
        [Fact]
        public async Task CreateAsync_Valid_ReturnsCreatedDto()
        {
            var dto = new CreateJobDto { EmployerId = Guid.NewGuid(), Title = "New", Description = "Desc" };
            var job = new Job { JobId = 5, Title = "New" };
            var resultDto = new JobDto { JobId = 5, Title = "New" };

            _mapperMock.Setup(m => m.Map<Job>(dto)).Returns(job);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Job>())).ReturnsAsync(job);
            _mapperMock.Setup(m => m.Map<JobDto>(job)).Returns(resultDto);

            var res = await _service.CreateAsync(dto);
            Assert.Equal("New", res.Title);
        }


        [Fact]
        public async Task UpdateAsync_Existing_ReturnsUpdatedDto()
        {
            var id = 2;
            var dto = new UpdateJobDto { Title = "Updated", Description = "Desc", Status = "Open" };
            var existing = new Job { JobId = id, Title = "Old" };
            var updated = new Job { JobId = id, Title = "Updated" };
            var resultDto = new JobDto { JobId = id, Title = "Updated" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _mapperMock.Setup(m => m.Map(dto, existing));
            _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(updated);
            _mapperMock.Setup(m => m.Map<JobDto>(updated)).Returns(resultDto);

            var res = await _service.UpdateAsync(id, dto);
            Assert.Equal("Updated", res.Title);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_Throws()
        {
            _repoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Job?)null);
            var dto = new UpdateJobDto { Title = "No" };
            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(9, dto));
        }

       
        [Fact]
        public async Task DeleteAsync_Existing_ReturnsTrue()
        {
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
            var res = await _service.DeleteAsync(1);
            Assert.True(res);
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(false);
            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(1));
        }
    }
}
