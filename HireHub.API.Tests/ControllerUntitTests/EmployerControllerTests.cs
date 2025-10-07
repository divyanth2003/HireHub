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
    public class EmployerControllerTests
    {
        private readonly Mock<IEmployerRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<EmployerService>> _loggerMock;
        private readonly EmployerService _service;
        private readonly EmployerController _controller;

        public EmployerControllerTests()
        {
            _repoMock = new Mock<IEmployerRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<EmployerService>>();

            _service = new EmployerService(
                _repoMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );

            _controller = new EmployerController(_service);
        }

       
        [Fact]
        public async Task GetAll_ReturnsOkWithEmployers()
        {
            var employers = new List<Employer> { new Employer { EmployerId = Guid.NewGuid(), CompanyName = "ABC" } };
            var dtos = new List<EmployerDto> { new EmployerDto { EmployerId = employers[0].EmployerId, CompanyName = "ABC" } };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(employers);
            _mapperMock.Setup(m => m.Map<IEnumerable<EmployerDto>>(employers)).Returns(dtos);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<IEnumerable<EmployerDto>>(ok.Value);
            Assert.Single(value);
        }


        [Fact]
        public async Task GetById_ExistingEmployer_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var employer = new Employer { EmployerId = id, CompanyName = "TechCorp" };
            var dto = new EmployerDto { EmployerId = id, CompanyName = "TechCorp" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(employer);
            _mapperMock.Setup(m => m.Map<EmployerDto>(employer)).Returns(dto);

            var result = await _controller.GetById(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<EmployerDto>(ok.Value);
            Assert.Equal(dto.CompanyName, val.CompanyName);
        }

        [Fact]
        public async Task GetById_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Employer?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetById(id));
        }

       
        [Fact]
        public async Task GetByUserId_ExistingEmployer_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            var employer = new Employer { EmployerId = Guid.NewGuid(), UserId = userId, CompanyName = "Xyz Pvt Ltd" };
            var dto = new EmployerDto { EmployerId = employer.EmployerId, CompanyName = "Xyz Pvt Ltd" };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(employer);
            _mapperMock.Setup(m => m.Map<EmployerDto>(employer)).Returns(dto);

            var result = await _controller.GetByUserId(userId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<EmployerDto>(ok.Value);
            Assert.Equal("Xyz Pvt Ltd", val.CompanyName);
        }

        [Fact]
        public async Task GetByUserId_NotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Employer?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetByUserId(userId));
        }

       
        [Fact]
        public async Task GetByJobId_Existing_ReturnsOk()
        {
            var jobId = 10;
            var employer = new Employer { EmployerId = Guid.NewGuid(), CompanyName = "SoftTech" };
            var dto = new EmployerDto { EmployerId = employer.EmployerId, CompanyName = "SoftTech" };

            _repoMock.Setup(r => r.GetByJobIdAsync(jobId)).ReturnsAsync(employer);
            _mapperMock.Setup(m => m.Map<EmployerDto>(employer)).Returns(dto);

            var result = await _controller.GetByJobId(jobId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<EmployerDto>(ok.Value);
            Assert.Equal("SoftTech", val.CompanyName);
        }

        [Fact]
        public async Task GetByJobId_NotFound_ThrowsNotFoundException()
        {
            var jobId = 99;
            _repoMock.Setup(r => r.GetByJobIdAsync(jobId)).ReturnsAsync((Employer?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetByJobId(jobId));
        }

 
        [Fact]
        public async Task SearchByCompany_ReturnsOk()
        {
            var query = "tech";
            var employers = new List<Employer> { new Employer { CompanyName = "Techify" } };
            var dtos = new List<EmployerDto> { new EmployerDto { CompanyName = "Techify" } };

            _repoMock.Setup(r => r.SearchByCompanyNameAsync(query)).ReturnsAsync(employers);
            _mapperMock.Setup(m => m.Map<IEnumerable<EmployerDto>>(employers)).Returns(dtos);

            var result = await _controller.SearchByCompany(query);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dtos, ok.Value);
        }

       
        [Fact]
        public async Task Create_ValidDto_ReturnsCreatedAtAction()
        {
            var dto = new CreateEmployerDto
            {
                CompanyName = "NewCo",
                UserId = Guid.NewGuid(),
                ContactInfo = "mail@newco.com"
            };

            var employer = new Employer { EmployerId = Guid.NewGuid(), CompanyName = dto.CompanyName, UserId = dto.UserId };
            var resultDto = new EmployerDto { EmployerId = employer.EmployerId, CompanyName = employer.CompanyName };

            _mapperMock.Setup(m => m.Map<Employer>(dto)).Returns(employer);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Employer>())).ReturnsAsync(employer);
            _mapperMock.Setup(m => m.Map<EmployerDto>(employer)).Returns(resultDto);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var val = Assert.IsType<EmployerDto>(created.Value);
            Assert.Equal(dto.CompanyName, val.CompanyName);
        }

        [Fact]
        public async Task Update_ExistingEmployer_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateEmployerDto { CompanyName = "UpdatedCo", ContactInfo = "updated@mail.com" };
            var employer = new Employer { EmployerId = id, CompanyName = "Old" };
            var updatedEmployer = new Employer { EmployerId = id, CompanyName = "UpdatedCo" };
            var resultDto = new EmployerDto { EmployerId = id, CompanyName = "UpdatedCo" };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(employer);
            _mapperMock.Setup(m => m.Map(dto, employer));
            _repoMock.Setup(r => r.UpdateAsync(employer)).ReturnsAsync(updatedEmployer);
            _mapperMock.Setup(m => m.Map<EmployerDto>(updatedEmployer)).Returns(resultDto);

            var result = await _controller.Update(id, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<EmployerDto>(ok.Value);
            Assert.Equal("UpdatedCo", val.CompanyName);
        }

        [Fact]
        public async Task Update_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateEmployerDto { CompanyName = "Nope" };
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Employer?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.Update(id, dto));
        }

        [Fact]
        public async Task Delete_Existing_ReturnsNoContent()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _controller.Delete(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => _controller.Delete(id));
        }
    }
}
