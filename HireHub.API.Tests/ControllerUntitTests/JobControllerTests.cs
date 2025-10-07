using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using HireHub.API.Controllers;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HireHub.API.Tests.Controllers
{
    public class JobControllerTests
    {
        private readonly Mock<IJobRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<JobService>> _loggerMock;
        private readonly JobService _service;
        private readonly JobController _controller;

        public JobControllerTests()
        {
            _repoMock = new Mock<IJobRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<JobService>>();

            _service = new JobService(_repoMock.Object, _mapperMock.Object, _loggerMock.Object);
            _controller = new JobController(_service);
        }

        // ------------------- GET ALL -------------------
        [Fact]
        public async Task GetAll_WhenCalled_ReturnsOkWithJobs()
        {
            var jobs = new List<Job> { new Job { JobId = 1, Title = "Software Engineer" } };
            var dtos = new List<JobDto> { new JobDto { JobId = 1, Title = "Software Engineer" } };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<JobDto>>(ok.Value);
            Assert.Single(list);
        }

        // ------------------- GET BY ID -------------------
        [Fact]
        public async Task GetById_Existing_ReturnsOk()
        {
            var job = new Job { JobId = 10, Title = "Dev" };
            var dto = new JobDto { JobId = 10, Title = "Dev" };

            _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(job);
            _mapperMock.Setup(m => m.Map<JobDto>(job)).Returns(dto);

            var result = await _controller.GetById(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<JobDto>(ok.Value);
            Assert.Equal(10, returned.JobId);
        }

        [Fact]
        public async Task GetById_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Job?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetById(99));
        }

        // ------------------- GET BY EMPLOYER -------------------
        [Fact]
        public async Task GetByEmployer_Existing_ReturnsOk()
        {
            var empId = Guid.NewGuid();
            var jobs = new List<Job> { new Job { JobId = 1, EmployerId = empId } };
            var dtos = new List<JobDto> { new JobDto { JobId = 1, EmployerId = empId } };

            _repoMock.Setup(r => r.GetByEmployerAsync(empId)).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var result = await _controller.GetByEmployer(empId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<JobDto>>(ok.Value);
            Assert.Single(list);
        }

        // ------------------- SEARCH -------------------
        [Fact]
        public async Task SearchByTitle_ReturnsOkWithResults()
        {
            var query = "Engineer";
            var jobs = new List<Job> { new Job { Title = "Engineer" } };
            var dtos = new List<JobDto> { new JobDto { Title = "Engineer" } };

            _repoMock.Setup(r => r.SearchByTitleAsync(query)).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var res = await _controller.SearchByTitle(query);

            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Equal(dtos, ok.Value);
        }

        [Fact]
        public async Task SearchByLocation_ReturnsOkWithResults()
        {
            var loc = "Chennai";
            var jobs = new List<Job> { new Job { Location = "Chennai" } };
            var dtos = new List<JobDto> { new JobDto { Location = "Chennai" } };

            _repoMock.Setup(r => r.SearchByLocationAsync(loc)).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var res = await _controller.SearchByLocation(loc);

            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Equal(dtos, ok.Value);
        }

        [Fact]
        public async Task SearchBySkill_ReturnsOkWithResults()
        {
            var skill = "C#";
            var jobs = new List<Job> { new Job { SkillsRequired = "C#" } };
            var dtos = new List<JobDto> { new JobDto { SkillsRequired = "C#" } };

            _repoMock.Setup(r => r.SearchBySkillAsync(skill)).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var res = await _controller.SearchBySkill(skill);

            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Equal(dtos, ok.Value);
        }

        [Fact]
        public async Task SearchByCompany_ReturnsOkWithResults()
        {
            var comp = "TechCorp";
            var jobs = new List<Job> { new Job { Title = "Intern" } };
            var dtos = new List<JobDto> { new JobDto { Title = "Intern" } };

            _repoMock.Setup(r => r.SearchByCompanyAsync(comp)).ReturnsAsync(jobs);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobDto>>(jobs)).Returns(dtos);

            var res = await _controller.SearchByCompany(comp);

            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Equal(dtos, ok.Value);
        }

        // ------------------- CREATE -------------------
        [Fact]
        public async Task Create_ValidDto_ReturnsCreatedAtAction()
        {
            var dto = new CreateJobDto { EmployerId = Guid.NewGuid(), Title = "Developer", Description = "Work" };
            var entity = new Job { JobId = 1, Title = "Developer" };
            var resultDto = new JobDto { JobId = 1, Title = "Developer" };

            _mapperMock.Setup(m => m.Map<Job>(dto)).Returns(entity);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Job>())).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<JobDto>(entity)).Returns(resultDto);

            var res = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(res);
            var val = Assert.IsType<JobDto>(created.Value);
            Assert.Equal("Developer", val.Title);
        }

        // ------------------- UPDATE -------------------
        [Fact]
        public async Task Update_Existing_ReturnsOk()
        {
            var id = 5;
            var dto = new UpdateJobDto { Title = "Updated", Description = "Desc", Status = "Open" };
            var job = new Job { JobId = id, Title = "Old" };
            var updated = new Job { JobId = id, Title = "Updated" };
            var resultDto = new JobDto { JobId = id, Title = "Updated" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(job);
            _mapperMock.Setup(m => m.Map(dto, job));
            _repoMock.Setup(r => r.UpdateAsync(job)).ReturnsAsync(updated);
            _mapperMock.Setup(m => m.Map<JobDto>(updated)).Returns(resultDto);

            var res = await _controller.Update(id, dto);
            var ok = Assert.IsType<OkObjectResult>(res);
            var val = Assert.IsType<JobDto>(ok.Value);
            Assert.Equal("Updated", val.Title);
        }

        [Fact]
        public async Task Update_NotFound_ThrowsNotFoundException()
        {
            var dto = new UpdateJobDto { Title = "No" };
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Job?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.Update(99, dto));
        }

        // ------------------- DELETE -------------------
        [Fact]
        public async Task Delete_Existing_ReturnsNoContent()
        {
            _repoMock.Setup(r => r.DeleteAsync(10)).ReturnsAsync(true);
            var result = await _controller.Delete(10);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.DeleteAsync(77)).ReturnsAsync(false);
            await Assert.ThrowsAsync<NotFoundException>(() => _controller.Delete(77));
        }
    }
}
