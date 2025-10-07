using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using HireHub.API.Controllers;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HireHub.API.Tests.Controllers
{
    public class ResumeControllerTests
    {
        private readonly Mock<IResumeRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ResumeService>> _serviceLoggerMock;
        private readonly Mock<ILogger<ResumeController>> _controllerLoggerMock;
        private readonly ResumeService _service;
        private readonly ResumeController _controller;

        public ResumeControllerTests()
        {
            _repoMock = new Mock<IResumeRepository>();
            _mapperMock = new Mock<IMapper>();
            _serviceLoggerMock = new Mock<ILogger<ResumeService>>();
            _controllerLoggerMock = new Mock<ILogger<ResumeController>>();

            _service = new ResumeService(_repoMock.Object, _mapperMock.Object, _serviceLoggerMock.Object);
            _controller = new ResumeController(_service, _controllerLoggerMock.Object);
        }

        // ------------------- GET -------------------
        [Fact]
        public async Task GetAll_WhenCalled_ReturnsOkWithResumes()
        {
            var resumes = new List<Resume> { new Resume { ResumeId = 1, ResumeName = "Resume1" } };
            var dtos = new List<ResumeDto> { new ResumeDto { ResumeId = 1, ResumeName = "Resume1" } };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(resumes);
            _mapperMock.Setup(m => m.Map<IEnumerable<ResumeDto>>(resumes)).Returns(dtos);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<IEnumerable<ResumeDto>>(ok.Value);
            Assert.Single(returned);
        }

        [Fact]
        public async Task GetById_Existing_ReturnsOk()
        {
            var resume = new Resume { ResumeId = 5, ResumeName = "CV" };
            var dto = new ResumeDto { ResumeId = 5, ResumeName = "CV" };

            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(resume);
            _mapperMock.Setup(m => m.Map<ResumeDto>(resume)).Returns(dto);

            var result = await _controller.GetById(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<ResumeDto>(ok.Value);
            Assert.Equal(5, val.ResumeId);
        }

        [Fact]
        public async Task GetById_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(77)).ReturnsAsync((Resume?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetById(77));
        }

        [Fact]
        public async Task GetByJobSeeker_Existing_ReturnsOk()
        {
            var jsId = Guid.NewGuid();
            var resumes = new List<Resume> { new Resume { ResumeId = 1, JobSeekerId = jsId } };
            var dtos = new List<ResumeDto> { new ResumeDto { ResumeId = 1, JobSeekerId = jsId } };

            _repoMock.Setup(r => r.GetByJobSeekerAsync(jsId)).ReturnsAsync(resumes);
            _mapperMock.Setup(m => m.Map<IEnumerable<ResumeDto>>(resumes)).Returns(dtos);

            var result = await _controller.GetByJobSeeker(jsId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<IEnumerable<ResumeDto>>(ok.Value);
            Assert.Single(returned);
        }

        [Fact]
        public async Task GetDefault_Existing_ReturnsOk()
        {
            var jsId = Guid.NewGuid();
            var resume = new Resume { ResumeId = 2, JobSeekerId = jsId, IsDefault = true };
            var dto = new ResumeDto { ResumeId = 2, JobSeekerId = jsId, IsDefault = true };

            _repoMock.Setup(r => r.GetDefaultByJobSeekerAsync(jsId)).ReturnsAsync(resume);
            _mapperMock.Setup(m => m.Map<ResumeDto>(resume)).Returns(dto);

            var result = await _controller.GetDefault(jsId);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task GetDefault_NoDefault_ReturnsNotFound()
        {
            var jsId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetDefaultByJobSeekerAsync(jsId)).ReturnsAsync((Resume?)null);
            var res = await _controller.GetDefault(jsId);
            Assert.IsType<NotFoundObjectResult>(res);
        }

        // ------------------- CREATE METADATA -------------------
        [Fact]
        public async Task CreateMetadata_Valid_ReturnsCreatedAtAction()
        {
            var dto = new CreateResumeDto { JobSeekerId = Guid.NewGuid(), ResumeName = "CV", FilePath = "path.pdf" };
            var created = new Resume { ResumeId = 1, ResumeName = "CV" };
            var resultDto = new ResumeDto { ResumeId = 1, ResumeName = "CV" };

            _mapperMock.Setup(m => m.Map<Resume>(dto)).Returns(created);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Resume>())).ReturnsAsync(created);
            _mapperMock.Setup(m => m.Map<ResumeDto>(created)).Returns(resultDto);

            var result = await _controller.CreateMetadata(dto);

            var createdRes = Assert.IsType<CreatedAtActionResult>(result);
            var val = Assert.IsType<ResumeDto>(createdRes.Value);
            Assert.Equal("CV", val.ResumeName);
        }

        [Fact]
        public async Task CreateMetadata_Duplicate_ReturnsConflictObjectResult()
        {
            // Arrange
            var jobSeekerId = Guid.NewGuid();
            var dto = new CreateResumeDto { JobSeekerId = jobSeekerId, ResumeName = "Dup", FilePath = "x.pdf" };

            // service will call repository.ExistsByNameAsync -> service throws DuplicateEmailException
            _repoMock.Setup(r => r.ExistsByNameAsync(dto.JobSeekerId, dto.ResumeName)).ReturnsAsync(true);

            // Act
            var result = await _controller.CreateMetadata(dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflict.Value);
            // optional: assert the message/key is present
            var objString = conflict.Value.ToString() ?? string.Empty;
            Assert.Contains("already exists", objString, StringComparison.OrdinalIgnoreCase);
        }


        // ------------------- UPDATE -------------------
        [Fact]
        public async Task Update_Existing_ReturnsOk()
        {
            var id = 1;
            var dto = new UpdateResumeDto { ResumeName = "New", FilePath = "path.pdf" };
            var resume = new Resume { ResumeId = id, ResumeName = "Old" };
            var updated = new Resume { ResumeId = id, ResumeName = "New" };
            var resultDto = new ResumeDto { ResumeId = id, ResumeName = "New" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(resume);
            _mapperMock.Setup(m => m.Map(dto, resume));
            _repoMock.Setup(r => r.UpdateAsync(resume)).ReturnsAsync(updated);
            _mapperMock.Setup(m => m.Map<ResumeDto>(updated)).Returns(resultDto);

            var res = await _controller.Update(id, dto);
            var ok = Assert.IsType<OkObjectResult>(res);
            var val = Assert.IsType<ResumeDto>(ok.Value);
            Assert.Equal("New", val.ResumeName);
        }

        [Fact]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            var dto = new UpdateResumeDto { ResumeName = "No", FilePath = "f.pdf" };
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Resume?)null);
            var res = await _controller.Update(99, dto);
            Assert.IsType<NotFoundObjectResult>(res);
        }

        // ------------------- DELETE -------------------
        [Fact]
        public async Task Delete_Existing_ReturnsNoContent()
        {
            var resume = new Resume { ResumeId = 1, FilePath = "Uploads/a.pdf" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(resume);
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var res = await _controller.Delete(1);
            Assert.IsType<NoContentResult>(res);
        }

        [Fact]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync((Resume?)null);
            var res = await _controller.Delete(11);
            Assert.IsType<NotFoundObjectResult>(res);
        }

        // ------------------- SET DEFAULT -------------------
        [Fact]
        public async Task SetDefault_Valid_ReturnsOk()
        {
            var jsId = Guid.NewGuid();
            var resume = new Resume { ResumeId = 1, JobSeekerId = jsId };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(resume);

            var res = await _controller.SetDefault(jsId, 1);
            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Contains("success", ok.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SetDefault_NotFound_ReturnsNotFound()
        {
            var jsId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((Resume?)null);

            var res = await _controller.SetDefault(jsId, 2);
            Assert.IsType<NotFoundObjectResult>(res);
        }
    }
}
