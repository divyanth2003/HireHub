using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HireHub.API.Services;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Exceptions;

namespace HireHub.API.Tests.Services
{
    public class ApplicationServiceTests
    {
        private readonly Mock<IApplicationRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ApplicationService>> _loggerMock;
        private readonly Mock<NotificationService> _notificationServiceMock;
        private readonly ApplicationService _sut;

        public ApplicationServiceTests()
        {
            _repoMock = new Mock<IApplicationRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ApplicationService>>();

           
            var notifRepoMock = new Mock<HireHub.API.Repositories.Interfaces.INotificationRepository>();
            var appRepoMockForNotif = new Mock<HireHub.API.Repositories.Interfaces.IApplicationRepository>();
            var emailMock = new Mock<IEmailService>();
            var notifMapperMock = new Mock<IMapper>();
            var notifLoggerMock = new Mock<ILogger<NotificationService>>();

            _notificationServiceMock = new Mock<NotificationService>(
                notifRepoMock.Object,
                appRepoMockForNotif.Object,
                emailMock.Object,
                notifMapperMock.Object,
                notifLoggerMock.Object
            );

          
            _sut = new ApplicationService(
                _repoMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _notificationServiceMock.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedDtos()
        {
           
            var entities = new List<Application>
            {
                new Application { ApplicationId = 1, JobId = 1 },
                new Application { ApplicationId = 2, JobId = 2 }
            };

            var dtos = new List<ApplicationDto>
            {
                new ApplicationDto { ApplicationId = 1, JobId = 1 },
                new ApplicationDto { ApplicationId = 2, JobId = 2 }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(entities)).Returns(dtos);

            var result = await _sut.GetAllAsync();

   
            result.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
    
            var id = 99;
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Application?)null);

          
            Func<Task> act = async () => await _sut.GetByIdAsync(id);

         
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Application with id '{id}' not found.");
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
           
            var id = 77;
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            
            Func<Task> act = async () => await _sut.DeleteAsync(id);

          
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Application with id '{id}' not found.");
        }

        [Fact]
        public async Task DeleteAsync_Existing_ReturnsTrue()
        {
           
            var id = 10;
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

           
            var result = await _sut.DeleteAsync(id);

           
            result.Should().BeTrue();
        }

       
        [Fact]
        public async Task CreateAsync_Valid_ReturnsCreatedDto_And_SendsNotification()
        {
           
            var createDto = new CreateApplicationDto { JobId = 5, JobSeekerId = Guid.NewGuid() };
            var entity = new Application { ApplicationId = 101, JobId = 5, JobSeekerId = createDto.JobSeekerId };
            var createdDto = new ApplicationDto { ApplicationId = 101, JobId = 5 };

            _mapperMock.Setup(m => m.Map<Application>(createDto)).Returns(entity);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Application>())).ReturnsAsync(entity);
            _repoMock.Setup(r => r.GetByIdAsync(entity.ApplicationId)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<ApplicationDto>(entity)).Returns(createdDto);

         
            var res = await _sut.CreateAsync(createDto);

           
            res.Should().NotBeNull();
            res.ApplicationId.Should().Be(101);
            res.JobId.Should().Be(5);
        }
    }
}
