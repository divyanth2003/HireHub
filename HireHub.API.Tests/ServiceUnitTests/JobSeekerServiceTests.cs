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
    public class JobSeekerServiceTests
    {
        private readonly Mock<IJobSeekerRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<JobSeekerService>> _loggerMock;
        private readonly JobSeekerService _service;

        public JobSeekerServiceTests()
        {
            _repoMock = new Mock<IJobSeekerRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<JobSeekerService>>();

            _service = new JobSeekerService(_repoMock.Object, _mapperMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_WhenCalled_ReturnsMappedDtos()
        {
            var list = new List<JobSeeker> { new JobSeeker { JobSeekerId = Guid.NewGuid() } };
            var dtos = new List<JobSeekerDto> { new JobSeekerDto { JobSeekerId = list[0].JobSeekerId } };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobSeekerDto>>(list)).Returns(dtos);

            var res = await _service.GetAllAsync();
            Assert.Single(res);
        }

        [Fact]
        public async Task GetByIdAsync_Existing_ReturnsMappedDto()
        {
            var id = Guid.NewGuid();
            var js = new JobSeeker { JobSeekerId = id };
            var dto = new JobSeekerDto { JobSeekerId = id };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(js);
            _mapperMock.Setup(m => m.Map<JobSeekerDto>(js)).Returns(dto);

            var res = await _service.GetByIdAsync(id);
            Assert.Equal(id, res.JobSeekerId);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((JobSeeker?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));
        }

        [Fact]
        public async Task GetByUserIdAsync_Existing_ReturnsMappedDto()
        {
            var uid = Guid.NewGuid();
            var js = new JobSeeker { JobSeekerId = Guid.NewGuid(), UserId = uid };
            var dto = new JobSeekerDto { JobSeekerId = js.JobSeekerId };

            _repoMock.Setup(r => r.GetByUserIdAsync(uid)).ReturnsAsync(js);
            _mapperMock.Setup(m => m.Map<JobSeekerDto>(js)).Returns(dto);

            var res = await _service.GetByUserIdAsync(uid);
            Assert.Equal(js.JobSeekerId, res.JobSeekerId);
        }

        [Fact]
        public async Task GetByUserIdAsync_NotFound_ThrowsNotFoundException()
        {
            var uid = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByUserIdAsync(uid)).ReturnsAsync((JobSeeker?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByUserIdAsync(uid));
        }

        [Fact]
        public async Task SearchByCollegeAsync_WhenCalled_ReturnsMappedDtos()
        {
            var college = "X";
            var list = new List<JobSeeker> { new JobSeeker { JobSeekerId = Guid.NewGuid(), College = college } };
            var dtos = new List<JobSeekerDto> { new JobSeekerDto { JobSeekerId = list[0].JobSeekerId } };

            _repoMock.Setup(r => r.SearchByCollegeAsync(college)).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobSeekerDto>>(list)).Returns(dtos);

            var res = await _service.SearchByCollegeAsync(college);
            Assert.Single(res);
        }

        [Fact]
        public async Task SearchBySkillAsync_WhenCalled_ReturnsMappedDtos()
        {
            var skill = "C#";
            var list = new List<JobSeeker> { new JobSeeker { JobSeekerId = Guid.NewGuid(), Skills = skill } };
            var dtos = new List<JobSeekerDto> { new JobSeekerDto { JobSeekerId = list[0].JobSeekerId } };

            _repoMock.Setup(r => r.SearchBySkillAsync(skill)).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<JobSeekerDto>>(list)).Returns(dtos);

            var res = await _service.SearchBySkillAsync(skill);
            Assert.Single(res);
        }

        [Fact]
        public async Task CreateAsync_Valid_ReturnsCreatedDto()
        {
            var dto = new CreateJobSeekerDto { UserId = Guid.NewGuid(), College = "C" };
            var mapped = new JobSeeker { JobSeekerId = Guid.NewGuid(), UserId = dto.UserId };
            var created = new JobSeeker { JobSeekerId = mapped.JobSeekerId, UserId = mapped.UserId };
            var outDto = new JobSeekerDto { JobSeekerId = created.JobSeekerId };

            _repoMock.Setup(r => r.ExistsForUserAsync(dto.UserId)).ReturnsAsync(false);
            _mapperMock.Setup(m => m.Map<JobSeeker>(dto)).Returns(mapped);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<JobSeeker>())).ReturnsAsync(created);
            _mapperMock.Setup(m => m.Map<JobSeekerDto>(created)).Returns(outDto);

            var res = await _service.CreateAsync(dto);
            Assert.Equal(outDto.JobSeekerId, res.JobSeekerId);
        }

        [Fact]
        public async Task CreateAsync_Duplicate_ThrowsDuplicateEmailException()
        {
            var dto = new CreateJobSeekerDto { UserId = Guid.NewGuid() };
            _repoMock.Setup(r => r.ExistsForUserAsync(dto.UserId)).ReturnsAsync(true);

            await Assert.ThrowsAsync<DuplicateEmailException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task UpdateAsync_Existing_ReturnsUpdatedDto()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateJobSeekerDto { College = "N" };
            var existing = new JobSeeker { JobSeekerId = id, College = "Old" };
            var updated = new JobSeeker { JobSeekerId = id, College = "N" };
            var outDto = new JobSeekerDto { JobSeekerId = id, College = "N" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _mapperMock.Setup(m => m.Map(dto, existing));
            _repoMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(updated);
            _mapperMock.Setup(m => m.Map<JobSeekerDto>(updated)).Returns(outDto);

            var res = await _service.UpdateAsync(id, dto);
            Assert.Equal("N", res.College);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateJobSeekerDto { College = "X" };
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((JobSeeker?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task DeleteAsync_Existing_NoDependents_ReturnsTrue()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new JobSeeker { JobSeekerId = id });
            _repoMock.Setup(r => r.HasDependentsAsync(id)).ReturnsAsync(false);
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var res = await _service.DeleteAsync(id);
            Assert.True(res);
            _repoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((JobSeeker?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
        }

        [Fact]
        public async Task DeleteAsync_HasDependents_ThrowsConflictException()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new JobSeeker { JobSeekerId = id });
            _repoMock.Setup(r => r.HasDependentsAsync(id)).ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(id));
        }

        [Fact]
        public async Task DeleteAsync_DeleteFails_ThrowsGenericException()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new JobSeeker { JobSeekerId = id });
            _repoMock.Setup(r => r.HasDependentsAsync(id)).ReturnsAsync(false);
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(id));
        }
    }
}
