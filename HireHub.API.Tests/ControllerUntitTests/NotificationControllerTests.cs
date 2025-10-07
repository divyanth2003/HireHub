using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HireHub.API.Controllers;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;

namespace HireHub.API.Tests.Controllers
{
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationRepository> _notifRepoMock;
        private readonly Mock<IApplicationRepository> _appRepoMock;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<NotificationService>> _loggerMock;
        private readonly NotificationService _notificationService;
        private readonly NotificationController _sut;

        public NotificationControllerTests()
        {
            _notifRepoMock = new Mock<INotificationRepository>();
            _appRepoMock = new Mock<IApplicationRepository>();
            _emailMock = new Mock<IEmailService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<NotificationService>>();

            _notificationService = new NotificationService(
                _notifRepoMock.Object,
                _appRepoMock.Object,
                _emailMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );

            _sut = new NotificationController(_notificationService);
        }

        [Fact]
        public async Task GetByUser_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            var entities = new List<Notification> { new Notification { NotificationId = 1, UserId = userId } };
            var dtos = new List<NotificationDto> { new NotificationDto { NotificationId = 1 } };

            _notifRepoMock.Setup(r => r.GetByUserAsync(userId)).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<NotificationDto>>(entities)).Returns(dtos);

            var result = await _sut.GetByUser(userId);

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetUnread_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            var entities = new List<Notification> { new Notification { NotificationId = 2 } };
            var dtos = new List<NotificationDto> { new NotificationDto { NotificationId = 2 } };

            _notifRepoMock.Setup(r => r.GetUnreadByUserAsync(userId)).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<NotificationDto>>(entities)).Returns(dtos);

            var result = await _sut.GetUnread(userId);

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetRecent_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            var entities = new List<Notification> { new Notification { NotificationId = 3 } };
            var dtos = new List<NotificationDto> { new NotificationDto { NotificationId = 3 } };

            _notifRepoMock.Setup(r => r.GetRecentByUserAsync(userId, 5)).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<NotificationDto>>(entities)).Returns(dtos);

            var result = await _sut.GetRecent(userId, 5);

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetById_ReturnsOk()
        {
            var id = 13;
            var entity = new Notification { NotificationId = id };
            var dto = new NotificationDto { NotificationId = id };

            _notifRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<NotificationDto>(entity)).Returns(dto);

            var result = await _sut.GetById(id);

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public async Task Create_ReturnsCreated()
        {
            var inDto = new CreateNotificationDto { UserId = Guid.NewGuid(), Message = "hi" };
            var entity = new Notification { NotificationId = 5, UserId = inDto.UserId, Message = inDto.Message };
            var outDto = new NotificationDto { NotificationId = 5, Message = inDto.Message };

            _mapperMock.Setup(m => m.Map<Notification>(inDto)).Returns(entity);
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>())).ReturnsAsync(entity);
            _notifRepoMock.Setup(r => r.GetByIdAsync(entity.NotificationId)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>())).Returns(outDto);

            var res = await _sut.Create(inDto);

            var created = res as CreatedAtActionResult;
            created.Should().NotBeNull();
            created!.Value.Should().BeEquivalentTo(outDto);
        }

        [Fact]
        public async Task Update_ReturnsOk()
        {
            var id = 22;
            var existing = new Notification { NotificationId = id, IsRead = false, Message = "old" };
            var dtoIn = new UpdateNotificationDto { IsRead = true, Message = "new" };
            var updated = new Notification { NotificationId = id, IsRead = true, Message = "new" };
            var outDto = new NotificationDto { NotificationId = id, IsRead = true, Message = "new" };

            _notifRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _notifRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>())).ReturnsAsync(updated);
            _notifRepoMock.Setup(r => r.GetByIdAsync(updated.NotificationId)).ReturnsAsync(updated);
            _mapperMock.Setup(m => m.Map<NotificationDto>(updated)).Returns(outDto);

            var res = await _sut.Update(id, dtoIn);

            var ok = res as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(outDto);
        }

        [Fact]
        public async Task MarkAsRead_ReturnsNoContent_WhenFound()
        {
            var id = 31;
            _notifRepoMock.Setup(r => r.MarkAsReadAsync(id)).ReturnsAsync(true);

            var res = await _sut.MarkAsRead(id);
            res.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task MarkAllAsRead_ReturnsOk_WithCount()
        {
            var userId = Guid.NewGuid();
            _notifRepoMock.Setup(r => r.MarkAllAsReadAsync(userId)).ReturnsAsync(5);

            var res = await _sut.MarkAllAsRead(userId);

            var ok = res as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(new { Updated = 5 }, options => options.ExcludingMissingMembers());
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            var id = 50;
            _notifRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var res = await _sut.Delete(id);
            res.Should().BeOfType<NoContentResult>();
        }
    }
}
