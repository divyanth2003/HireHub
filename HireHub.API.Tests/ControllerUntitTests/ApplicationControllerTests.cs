using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using HireHub.API.Controllers;
using HireHub.API.Services;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;

namespace HireHub.API.Tests.Controllers
{
    public class ApplicationControllerTests
    {
        private readonly Mock<ApplicationService> _serviceMock;
        private readonly ApplicationController _sut;

        public ApplicationControllerTests()
        {
            // Mocking a concrete ApplicationService can fail if it has no parameterless constructor.
            // Instead we create a Mock<ApplicationService> but using loose behavior and bypass constructor parameters.
            _serviceMock = new Mock<ApplicationService>(MockBehavior.Strict, null!, null!, null!, null!);
            _sut = new ApplicationController(_serviceMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkObjectResult()
        {
            var list = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 1 } };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(list);

            var res = await _sut.GetAll();

            res.Should().BeOfType<OkObjectResult>();
            var ok = res as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(list);
        }

        [Fact]
        public async Task GetById_Existing_ReturnsOk()
        {
            var dto = new ApplicationDto { ApplicationId = 5 };
            _serviceMock.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(dto);

            var res = await _sut.GetById(5);

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public async Task GetById_NotFound_ThrowsNotFoundException()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(99)).ThrowsAsync(new NotFoundException("Application with id '99' not found."));

            Func<Task> act = async () => await _sut.GetById(99);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetByJob_ReturnsOk()
        {
            var list = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 2 } };
            _serviceMock.Setup(s => s.GetByJobAsync(10)).ReturnsAsync(list);

            var res = await _sut.GetByJob(10);

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(list);
        }

        [Fact]
        public async Task GetByJobSeeker_ReturnsOk()
        {
            var jsId = Guid.NewGuid();
            var list = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 3 } };
            _serviceMock.Setup(s => s.GetByJobSeekerAsync(jsId)).ReturnsAsync(list);

            var res = await _sut.GetByJobSeeker(jsId);

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(list);
        }

        [Fact]
        public async Task GetShortlisted_ReturnsOk()
        {
            var list = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 4 } };
            _serviceMock.Setup(s => s.GetShortlistedByJobAsync(12)).ReturnsAsync(list);

            var res = await _sut.GetShortlisted(12);

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(list);
        }

        [Fact]
        public async Task GetWithInterview_ReturnsOk()
        {
            var list = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 6 } };
            _serviceMock.Setup(s => s.GetWithInterviewAsync(13)).ReturnsAsync(list);

            var res = await _sut.GetWithInterview(13);

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(list);
        }

        [Fact]
        public async Task Create_Valid_ReturnsCreatedAt()
        {
            var create = new CreateApplicationDto { JobId = 10, JobSeekerId = Guid.NewGuid() };
            var created = new ApplicationDto { ApplicationId = 101 };

            _serviceMock.Setup(s => s.CreateAsync(create)).ReturnsAsync(created);

            var res = await _sut.Create(create);

            res.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = res as CreatedAtActionResult;
            createdResult!.Value.Should().BeEquivalentTo(created);
        }

        [Fact]
        public async Task Update_Valid_ReturnsOk()
        {
            var id = 7;
            var dto = new UpdateApplicationDto { Status = "Shortlisted" };
            var updated = new ApplicationDto { ApplicationId = id, Status = "Shortlisted" };

            _serviceMock.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

            var res = await _sut.Update(id, dto);

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(updated);
        }

        [Fact]
        public async Task Delete_Existing_ReturnsNoContent()
        {
            _serviceMock.Setup(s => s.DeleteAsync(9)).ReturnsAsync(true);

            var res = await _sut.Delete(9);

            res.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_NotFound_ThrowsNotFoundException()
        {
            _serviceMock.Setup(s => s.DeleteAsync(999)).ThrowsAsync(new NotFoundException("Application with id '999' not found."));

            Func<Task> act = async () => await _sut.Delete(999);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task MarkReviewed_Existing_ReturnsOk()
        {
            var appId = 20;
            var dto = new ApplicationDto { ApplicationId = appId };
            _serviceMock.Setup(s => s.MarkReviewedAsync(appId, "notes")).ReturnsAsync(dto);

            var res = await _sut.MarkReviewed(appId, "notes");

            res.Should().BeOfType<OkObjectResult>();
            (res as OkObjectResult)!.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public async Task MarkReviewed_NotFound_ThrowsNotFoundException()
        {
            _serviceMock.Setup(s => s.MarkReviewedAsync(999, null)).ThrowsAsync(new NotFoundException("Application with id '999' not found."));
            Func<Task> act = async () => await _sut.MarkReviewed(999, null);
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
