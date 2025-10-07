using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HireHub.API.Controllers;
using HireHub.API.DTOs;
using HireHub.API.Services;
using HireHub.API.Exceptions;

namespace HireHub.API.Tests.Controllers
{
    public class ApplicationControllerTests
    {
        private readonly Mock<ApplicationService> _applicationServiceMock;
        private readonly ApplicationController _sut;

        public ApplicationControllerTests()
        {
            // Create mocks for ApplicationService constructor dependencies
            var repoMock = new Mock<HireHub.API.Repositories.Interfaces.IApplicationRepository>();
            var mapperMock = new Mock<AutoMapper.IMapper>();
            var loggerMock = new Mock<ILogger<ApplicationService>>();

            // NotificationService constructor requires dependencies, so create mocks for those too
            var notifRepoMock = new Mock<HireHub.API.Repositories.Interfaces.INotificationRepository>();
            var appRepoMockForNotif = new Mock<HireHub.API.Repositories.Interfaces.IApplicationRepository>();
            var emailMock = new Mock<IEmailService>();
            var notifMapperMock = new Mock<AutoMapper.IMapper>();
            var notifLoggerMock = new Mock<ILogger<NotificationService>>();

            var notificationServiceMock = new Mock<NotificationService>(
                notifRepoMock.Object,
                appRepoMockForNotif.Object,
                emailMock.Object,
                notifMapperMock.Object,
                notifLoggerMock.Object
            );

            // Now create a Mock<ApplicationService> by supplying valid constructor args
            _applicationServiceMock = new Mock<ApplicationService>(
                repoMock.Object,
                mapperMock.Object,
                loggerMock.Object,
                notificationServiceMock.Object
            );

            // Controller depends on concrete ApplicationService (not an interface),
            // so we pass the mocked object.
            _sut = new ApplicationController(_applicationServiceMock.Object);
        }

        [Fact]
        public async Task GetById_Existing_ReturnsOkObjectResult()
        {
            // Arrange
            var id = 11;
            var dto = new ApplicationDto { ApplicationId = id, JobTitle = "X" };
            _applicationServiceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(dto);

            // Act
            var result = await _sut.GetById(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ok.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public async Task GetById_NotFound_ThrowsNotFoundException_Propagates()
        {
            // Arrange
            var id = 999;
            _applicationServiceMock
                .Setup(s => s.GetByIdAsync(id))
                .ThrowsAsync(new NotFoundException($"Application with id '{id}' not found."));

            // Act
            Func<Task> act = async () => await _sut.GetById(id);

            // Assert - controller currently doesn't catch NotFoundException for this action,
            // so exception will bubble to test. We assert it.
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Create_Valid_ReturnsCreatedAtAction()
        {
            // Arrange
            var create = new CreateApplicationDto { JobId = 1, JobSeekerId = Guid.NewGuid() };
            var created = new ApplicationDto { ApplicationId = 123, JobTitle = "Role" };

            _applicationServiceMock
                .Setup(s => s.CreateAsync(create))
                .ReturnsAsync(created);

            // Act
            var result = await _sut.Create(create);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = (CreatedAtActionResult)result;
            createdResult.Value.Should().BeEquivalentTo(created);
            createdResult.RouteValues.Should().ContainKey("id");
            createdResult.RouteValues["id"].Should().Be(created.ApplicationId);
        }

        [Fact]
        public async Task Delete_Existing_ReturnsNoContent()
        {
            // Arrange
            var id = 10;
            _applicationServiceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _sut.Delete(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_NotFound_ThrowsNotFoundException()
        {
            // Arrange
            var id = 999;
        _application_service_setup_throw_notfound:
            _applicationServiceMock
                .Setup(s => s.DeleteAsync(id))
                .ThrowsAsync(new NotFoundException($"Application with id '{id}' not found."));

            // Act
            Func<Task> act = async () => await _sut.Delete(id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
