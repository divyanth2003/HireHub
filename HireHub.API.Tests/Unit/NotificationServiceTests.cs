using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using HireHub.API.Exceptions;

namespace HireHub.API.Tests.Unit
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _repoMock;
        private readonly Mock<IApplicationRepository> _applicationRepoMock; // NEW
        private readonly Mock<IEmailService> _emailMock;                    // NEW
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<NotificationService>> _loggerMock;
        private readonly NotificationService _sut;

        public NotificationServiceTests()
        {
            _repoMock = new Mock<INotificationRepository>();
            _applicationRepoMock = new Mock<IApplicationRepository>();
            _emailMock = new Mock<IEmailService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<NotificationService>>();

            // construct SUT with all dependencies (order matters)
            _sut = new NotificationService(
                _repoMock.Object,
                _applicationRepoMock.Object,
                _emailMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetByUserAsync_ShouldReturnMappedList()
        {
            var userId = Guid.NewGuid();
            var entities = new List<Notification>
            {
                new Notification { NotificationId = 1, UserId = userId, Message = "A", CreatedAt = DateTime.UtcNow },
                new Notification { NotificationId = 2, UserId = userId, Message = "B", CreatedAt = DateTime.UtcNow }
            };
            var dtos = new List<NotificationDto>
            {
                new NotificationDto { NotificationId = 1, UserId = userId, Message = "A", CreatedAt = DateTime.UtcNow },
                new NotificationDto { NotificationId = 2, UserId = userId, Message = "B", CreatedAt = DateTime.UtcNow }
            };

            _repoMock.Setup(r => r.GetByUserAsync(userId)).ReturnsAsync(entities);
            // We can't use exact instance match on Map overload easily, so use It.IsAny<>
            _mapperMock.Setup(m => m.Map<IEnumerable<NotificationDto>>(It.IsAny<IEnumerable<Notification>>()))
                       .Returns(dtos);

            var result = await _sut.GetByUserAsync(userId);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenMissing()
        {
            var id = 5;
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Notification?)null);

            var act = async () => await _sut.GetByIdAsync(id);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Notification with id '{id}' not found.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnMappedDto_WhenCreated()
        {
            var createDto = new CreateNotificationDto
            {
                UserId = Guid.NewGuid(),
                Message = "Hello"
            };

            var entity = new Notification
            {
                NotificationId = 11,
                UserId = createDto.UserId,
                Message = createDto.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var returnedDto = new NotificationDto
            {
                NotificationId = entity.NotificationId,
                UserId = entity.UserId,
                Message = entity.Message,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };

            // Map from CreateNotificationDto to Notification (service uses mapper)
            _mapperMock.Setup(m => m.Map<Notification>(It.IsAny<CreateNotificationDto>())).Returns(entity);

            // Repo returns the entity on AddAsync
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Notification>())).ReturnsAsync(entity);

            // After create the service asks repo for GetByIdAsync to include navigation -> return entity
            _repoMock.Setup(r => r.GetByIdAsync(entity.NotificationId)).ReturnsAsync(entity);

            // Map Notification -> NotificationDto for returning to caller
            _mapperMock.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>())).Returns(returnedDto);

            var result = await _sut.CreateAsync(createDto);

            result.Should().NotBeNull();
            result.NotificationId.Should().Be(entity.NotificationId);
            result.Message.Should().Be(createDto.Message);
            result.IsRead.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenMissing()
        {
            var id = 7;
            var updateDto = new UpdateNotificationDto { IsRead = true };
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Notification?)null);

            var act = async () => await _sut.UpdateAsync(id, updateDto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Notification with id '{id}' not found.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnMappedDto_WhenUpdated()
        {
            var id = 8;
            var existing = new Notification
            {
                NotificationId = id,
                UserId = Guid.NewGuid(),
                Message = "hi",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var updatedEntity = new Notification
            {
                NotificationId = id,
                UserId = existing.UserId,
                Message = existing.Message,
                IsRead = true,
                CreatedAt = existing.CreatedAt
            };

            var returnedDto = new NotificationDto
            {
                NotificationId = id,
                UserId = existing.UserId,
                Message = existing.Message,
                IsRead = true,
                CreatedAt = existing.CreatedAt
            };

            var updateDto = new UpdateNotificationDto { IsRead = true };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            // simulate repo update returning updated entity
            _repoMock.Setup(r => r.UpdateAsync(It.Is<Notification>(n => n.NotificationId == id)))
                     .ReturnsAsync(updatedEntity);
            _repoMock.Setup(r => r.GetByIdAsync(updatedEntity.NotificationId)).ReturnsAsync(updatedEntity);
            _mapperMock.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>())).Returns(returnedDto);

            var result = await _sut.UpdateAsync(id, updateDto);

            result.Should().NotBeNull();
            result.IsRead.Should().BeTrue();
            result.NotificationId.Should().Be(id);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenDeleteReturnsFalse()
        {
            var id = 99;
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            var act = async () => await _sut.DeleteAsync(id);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Notification with id '{id}' not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenDeleted()
        {
            var id = 10;
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _sut.DeleteAsync(id);

            result.Should().BeTrue();
        }
    }
}
