using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HireHub.API.Tests.Services
{
    public class ResumeServiceTests
    {
        private readonly Mock<IResumeRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ResumeService>> _loggerMock;
        private readonly ResumeService _service;

        public ResumeServiceTests()
        {
            _repoMock = new Mock<IResumeRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ResumeService>>();
            _service = new ResumeService(_repoMock.Object, _mapperMock.Object, _loggerMock.Object);
        }

   
        [Fact]
        public async Task GetAllAsync_WhenCalled_ReturnsDtos()
        {
            var list = new List<Resume> { new Resume { ResumeId = 1, ResumeName = "CV" } };
            var dtos = new List<ResumeDto> { new ResumeDto { ResumeId = 1, ResumeName = "CV" } };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<ResumeDto>>(list)).Returns(dtos);

            var res = await _service.GetAllAsync();
            Assert.Single(res);
        }

        [Fact]
        public async Task GetByIdAsync_Existing_ReturnsDto()
        {
            var resume = new Resume { ResumeId = 5 };
            var dto = new ResumeDto { ResumeId = 5 };

            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(resume);
            _mapperMock.Setup(m => m.Map<ResumeDto>(resume)).Returns(dto);

            var result = await _service.GetByIdAsync(5);
            Assert.Equal(5, result.ResumeId);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_Throws()
        {
            _repoMock.Setup(r => r.GetByIdAsync(77)).ReturnsAsync((Resume?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(77));
        }

        [Fact]
        public async Task GetByJobSeekerAsync_WhenCalled_ReturnsDtos()
        {
            var jsId = Guid.NewGuid();
            var list = new List<Resume> { new Resume { JobSeekerId = jsId } };
            var dtos = new List<ResumeDto> { new ResumeDto { JobSeekerId = jsId } };

            _repoMock.Setup(r => r.GetByJobSeekerAsync(jsId)).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<ResumeDto>>(list)).Returns(dtos);

            var res = await _service.GetByJobSeekerAsync(jsId);
            Assert.Single(res);
        }

        [Fact]
        public async Task GetDefaultByJobSeekerAsync_WhenFound_ReturnsDto()
        {
            var jsId = Guid.NewGuid();
            var resume = new Resume { ResumeId = 1, JobSeekerId = jsId, IsDefault = true };
            var dto = new ResumeDto { ResumeId = 1, IsDefault = true };

            _repoMock.Setup(r => r.GetDefaultByJobSeekerAsync(jsId)).ReturnsAsync(resume);
            _mapperMock.Setup(m => m.Map<ResumeDto>(resume)).Returns(dto);

            var res = await _service.GetDefaultByJobSeekerAsync(jsId);
            Assert.True(res.IsDefault);
        }

        [Fact]
        public async Task GetDefaultByJobSeekerAsync_NotFound_ReturnsNull()
        {
            var jsId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetDefaultByJobSeekerAsync(jsId)).ReturnsAsync((Resume?)null);

            var res = await _service.GetDefaultByJobSeekerAsync(jsId);
            Assert.Null(res);
        }


        [Fact]
        public async Task CreateAsync_Valid_ReturnsDto()
        {
            var dto = new CreateResumeDto { JobSeekerId = Guid.NewGuid(), ResumeName = "CV", FilePath = "f.pdf" };
            var resume = new Resume { ResumeId = 1, ResumeName = "CV" };
            var resultDto = new ResumeDto { ResumeId = 1, ResumeName = "CV" };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.JobSeekerId, dto.ResumeName)).ReturnsAsync(false);
            _mapperMock.Setup(m => m.Map<Resume>(dto)).Returns(resume);
            _repoMock.Setup(r => r.AddAsync(resume)).ReturnsAsync(resume);
            _mapperMock.Setup(m => m.Map<ResumeDto>(resume)).Returns(resultDto);

            var res = await _service.CreateAsync(dto);
            Assert.Equal("CV", res.ResumeName);
        }

        [Fact]
        public async Task CreateAsync_Duplicate_Throws()
        {
            var dto = new CreateResumeDto { JobSeekerId = Guid.NewGuid(), ResumeName = "Dup" };
            _repoMock.Setup(r => r.ExistsByNameAsync(dto.JobSeekerId, dto.ResumeName)).ReturnsAsync(true);
            await Assert.ThrowsAsync<DuplicateEmailException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task UpdateAsync_Existing_ReturnsDto()
        {
            var id = 5;
            var dto = new UpdateResumeDto { ResumeName = "Updated", FilePath = "file.pdf" };
            var resume = new Resume { ResumeId = id, ResumeName = "Old" };
            var updated = new Resume { ResumeId = id, ResumeName = "Updated" };
            var resultDto = new ResumeDto { ResumeId = id, ResumeName = "Updated" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(resume);
            _mapperMock.Setup(m => m.Map(dto, resume));
            _repoMock.Setup(r => r.UpdateAsync(resume)).ReturnsAsync(updated);
            _mapperMock.Setup(m => m.Map<ResumeDto>(updated)).Returns(resultDto);

            var res = await _service.UpdateAsync(id, dto);
            Assert.Equal("Updated", res.ResumeName);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_Throws()
        {
            var dto = new UpdateResumeDto { ResumeName = "None", FilePath = "f.pdf" };
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Resume?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(99, dto));
        }

        [Fact]
        public async Task DeleteAsync_Existing_ReturnsTrue()
        {
            var resume = new Resume { ResumeId = 1, FilePath = "Uploads/test.pdf" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(resume);
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var res = await _service.DeleteAsync(1);
            Assert.True(res);
        }

        [Fact]
        public async Task DeleteAsync_NotFound_Throws()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Resume?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(1));
        }

        [Fact]
        public async Task DeleteAsync_DbConflict_ThrowsConflictException()
        {
            var resume = new Resume { ResumeId = 5, FilePath = "Uploads/x.pdf" };
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(resume);
            _repoMock.Setup(r => r.DeleteAsync(5)).ThrowsAsync(new DbUpdateException());

            await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(5));
        }

 
        [Fact]
        public async Task SetDefault_Valid_ReturnsTrue()
        {
       
            var jsId = Guid.NewGuid();
            var resumeId = 1;

            
            var resume = new Resume { ResumeId = resumeId, JobSeekerId = jsId };

            _repoMock.Setup(r => r.GetByIdAsync(resumeId)).ReturnsAsync(resume);
            _repoMock.Setup(r => r.SetDefaultAsync(jsId, resumeId)).Returns(Task.CompletedTask);

         
            var res = await _service.SetDefaultAsync(jsId, resumeId);

           
            Assert.True(res);
            _repoMock.Verify(r => r.SetDefaultAsync(jsId, resumeId), Times.Once);
        }


        [Fact]
        public async Task SetDefault_NotFound_Throws()
        {
            var jsId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Resume?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.SetDefaultAsync(jsId, 9));
        }
    }
}


