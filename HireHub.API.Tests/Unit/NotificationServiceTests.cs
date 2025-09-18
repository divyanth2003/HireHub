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
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<NotificationService>> _loggerMock;
        private readonly NotificationService _sut;

        public NotificationServiceTests()
        {
            _repoMock = new Mock<INotificationRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<NotificationService>>();

            _sut = new NotificationService(
                _repoMock.Object,
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
                new Notification { NotificationId = 1, UserId = userId, Message = "A" },
                new Notification { NotificationId = 2, UserId = userId, Message = "B" }
            };
            var dtos = new List<NotificationDto>
            {
                new NotificationDto { NotificationId = 1, UserId = userId, Message = "A" },
                new NotificationDto { NotificationId = 2, UserId = userId, Message = "B" }
            };

            _repoMock.Setup(r => r.GetByUserAsync(userId)).ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<NotificationDto>>(entities)).Returns(dtos);

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
                IsRead = entity.IsRead
            };

            _mapperMock.Setup(m => m.Map<Notification>(createDto)).Returns(entity);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Notification>())).ReturnsAsync(entity);
            _repoMock.Setup(r => r.GetByIdAsync(entity.NotificationId)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<NotificationDto>(entity)).Returns(returnedDto);

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
                IsRead = false
            };

            var updatedEntity = new Notification
            {
                NotificationId = id,
                UserId = existing.UserId,
                Message = existing.Message,
                IsRead = true
            };

            var returnedDto = new NotificationDto
            {
                NotificationId = id,
                UserId = existing.UserId,
                Message = existing.Message,
                IsRead = true
            };

            var updateDto = new UpdateNotificationDto { IsRead = true };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            // simulate repo update returning updated entity
            _repoMock.Setup(r => r.UpdateAsync(It.Is<Notification>(n => n.NotificationId == id)))
                     .ReturnsAsync(updatedEntity);
            _repoMock.Setup(r => r.GetByIdAsync(updatedEntity.NotificationId)).ReturnsAsync(updatedEntity);
            _mapperMock.Setup(m => m.Map<NotificationDto>(updatedEntity)).Returns(returnedDto);

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