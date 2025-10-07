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
    public class JobSeekerControllerTests
    {
        private readonly Mock<IJobSeekerRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<JobSeekerService>> _serviceLoggerMock;
        private readonly Mock<ILogger<JobSeekerController>> _controllerLoggerMock;
        private readonly JobSeekerService _service;
        private readonly JobSeekerController _controller;

        public JobSeekerControllerTests()
        {
            _repoMock = new Mock<IJobSeekerRepository>();
            _mapperMock = new Mock<IMapper>();
            _serviceLoggerMock = new Mock<ILogger<JobSeekerService>>();
            _controllerLoggerMock = new Mock<ILogger<JobSeekerController>>();

            _service = new JobSeekerService(_repoMock.Object, _mapperMock.Object, _serviceLoggerMock.Object);
            _controller = new JobSeekerController(_service, _controllerLoggerMock.Object);
        }

        // GET ALL
        [Fact]
        public async Task GetAll_WhenCalled_ReturnsOkWithList()
        {
            var jsList = new List<JobSeeker> { new JobSeeker { JobSeekerId = Guid.NewGuid(), UserId = Guid.NewGuid() } };
            var dtoList = new List<JobSeekerDto> { new JobSeekerDto { JobSeekerId = jsList[0].JobSeekerId } };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(jsList);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobSeekerDto>>(jsList)).Returns(dtoList);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<IEnumerable<JobSeekerDto>>(ok.Value);
            Assert.Single(returned);
        }

        // GET BY ID - success
        [Fact]
        public async Task GetById_ExistingId_ReturnsOkWithDto()
        {
            var id = Guid.NewGuid();
            var js = new JobSeeker { JobSeekerId = id, UserId = Guid.NewGuid() };
            var dto = new JobSeekerDto { JobSeekerId = id };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(js);
            _mapperMock.Setup(m => m.Map<JobSeekerDto>(js)).Returns(dto);

            var result = await _controller.GetById(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<JobSeekerDto>(ok.Value);
            Assert.Equal(id, returned.JobSeekerId);
        }

        // GET BY ID - not found -> controller will propagate exception (service throws), so test expects exception
        [Fact]
        public async Task GetById_NonExisting_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((JobSeeker?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetById(id));
        }

        // GET BY USERID - success
        [Fact]
        public async Task GetByUserId_Existing_ReturnsOkWithDto()
        {
            var userId = Guid.NewGuid();
            var js = new JobSeeker { JobSeekerId = Guid.NewGuid(), UserId = userId };
            var dto = new JobSeekerDto { JobSeekerId = js.JobSeekerId };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(js);
            _mapperMock.Setup(m => m.Map<JobSeekerDto>(js)).Returns(dto);

            var result = await _controller.GetByUserId(userId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<JobSeekerDto>(ok.Value);
            Assert.Equal(js.JobSeekerId, returned.JobSeekerId);
        }

        [Fact]
        public async Task GetByUserId_NotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((JobSeeker?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetByUserId(userId));
        }

        // SEARCH BY COLLEGE
        [Fact]
        public async Task SearchByCollege_WhenCalled_ReturnsOkWithResults()
        {
            var college = "CollegeX";
            var jsList = new List<JobSeeker> { new JobSeeker { JobSeekerId = Guid.NewGuid(), College = "CollegeX" } };
            var dtoList = new List<JobSeekerDto> { new JobSeekerDto { JobSeekerId = jsList[0].JobSeekerId } };

            _repoMock.Setup(r => r.SearchByCollegeAsync(college)).ReturnsAsync(jsList);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobSeekerDto>>(jsList)).Returns(dtoList);

            var result = await _controller.SearchByCollege(college);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dtoList, ok.Value);
        }

        // SEARCH BY SKILL
        [Fact]
        public async Task SearchBySkill_WhenCalled_ReturnsOkWithResults()
        {
            var skill = "C#";
            var jsList = new List<JobSeeker> { new JobSeeker { JobSeekerId = Guid.NewGuid(), Skills = "C#,ASP.NET" } };
            var dtoList = new List<JobSeekerDto> { new JobSeekerDto { JobSeekerId = jsList[0].JobSeekerId } };

            _repoMock.Setup(r => r.SearchBySkillAsync(skill)).ReturnsAsync(jsList);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobSeekerDto>>(jsList)).Returns(dtoList);

            var result = await _controller.SearchBySkill(skill);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dtoList, ok.Value);
        }

        // CREATE - success
        [Fact]
        public async Task Create_ValidDto_ReturnsCreatedAtAction()
        {
            var dto = new CreateJobSeekerDto { UserId = Guid.NewGuid(), College = "ABC" };
            var mapped = new JobSeeker { JobSeekerId = Guid.NewGuid(), UserId = dto.UserId };
            var created = new JobSeeker { JobSeekerId = mapped.JobSeekerId, UserId = mapped.UserId };
            var returnedDto = new JobSeekerDto { JobSeekerId = created.JobSeekerId, UserId = created.UserId };

            _repoMock.Setup(r => r.ExistsForUserAsync(dto.UserId)).ReturnsAsync(false);
            _mapperMock.Setup(m => m.Map<JobSeeker>(dto)).Returns(mapped);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<JobSeeker>())).ReturnsAsync(created);
            _mapperMock.Setup(m => m.Map<JobSeekerDto>(created)).Returns(returnedDto);

            var res = await _controller.Create(dto);
            var createdAt = Assert.IsType<CreatedAtActionResult>(res);
            var val = Assert.IsType<JobSeekerDto>(createdAt.Value);
            Assert.Equal(returnedDto.JobSeekerId, val.JobSeekerId);
        }

        // CREATE - duplicate -> service will throw DuplicateEmailException (used for duplicate jobseeker)
        [Fact]
        public async Task Create_DuplicateForUser_ThrowsDuplicateEmailException()
        {
            var dto = new CreateJobSeekerDto { UserId = Guid.NewGuid() };
            _repoMock.Setup(r => r.ExistsForUserAsync(dto.UserId)).ReturnsAsync(true);

            await Assert.ThrowsAsync<DuplicateEmailException>(() => _controller.Create(dto));
        }

        // UPDATE - success
        [Fact]
        public async Task Update_Existing_ReturnsOkWithUpdatedDto()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateJobSeekerDto { College = "NewCollege", Skills = "X" };
            var existing = new JobSeeker { JobSeekerId = id, College = "Old" };
            var updated = new JobSeeker { JobSeekerId = id, College = "NewCollege" };
            var returnedDto = new JobSeekerDto { JobSeekerId = id, College = "NewCollege" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _mapperMock.Setup(m => m.Map(dto, existing));
            _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(updated);
            _mapperMock.Setup(m => m.Map<JobSeekerDto>(updated)).Returns(returnedDto);

            var res = await _controller.Update(id, dto);
            var ok = Assert.IsType<OkObjectResult>(res);
            var val = Assert.IsType<JobSeekerDto>(ok.Value);
            Assert.Equal("NewCollege", val.College);
        }

        // UPDATE - not found -> service throws NotFoundException
        [Fact]
        public async Task Update_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateJobSeekerDto { College = "N" };
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((JobSeeker?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.Update(id, dto));
        }

        // DELETE - success -> controller returns NoContent
        [Fact]
        public async Task Delete_Existing_ReturnsNoContent()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new JobSeeker { JobSeekerId = id });
            _repoMock.Setup(r => r.HasDependentsAsync(id)).ReturnsAsync(false);
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var res = await _controller.Delete(id);
            Assert.IsType<NoContentResult>(res);
        }

        // DELETE - not found -> controller catches NotFoundException and returns NotFoundObjectResult
        [Fact]
        public async Task Delete_NotFound_ReturnsNotFoundObjectResult()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((JobSeeker?)null);

            var res = await _controller.Delete(id);
            var notFound = Assert.IsType<NotFoundObjectResult>(res);
            Assert.Contains("not found", notFound.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // DELETE - conflict due to dependents -> returns ConflictObjectResult
        [Fact]
        public async Task Delete_HasDependents_ReturnsConflictObjectResult()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new JobSeeker { JobSeekerId = id });
            _repoMock.Setup(r => r.HasDependentsAsync(id)).ReturnsAsync(true);

            var res = await _controller.Delete(id);
            var conflict = Assert.IsType<ConflictObjectResult>(res);
            Assert.Contains("dependent", conflict.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
