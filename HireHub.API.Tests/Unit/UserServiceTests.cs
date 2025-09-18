using System;
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
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _sut; // System Under Test

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _mapperMock = new Mock<IMapper>();
            _tokenServiceMock = new Mock<ITokenService>();
            _loggerMock = new Mock<ILogger<UserService>>();

            _sut = new UserService(
                _userRepoMock.Object,
                _mapperMock.Object,
                _tokenServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowDuplicateEmailException_WhenEmailAlreadyExists()
        {
            // Arrange
            var dto = new CreateUserDto { Email = "test@example.com", Password = "Pass@123", FullName = "Test", Role = "JobSeeker" };
            var existingUser = new User { Email = "test@example.com" };

            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email))
                         .ReturnsAsync(existingUser);

            // Act
            Func<Task> act = async () => await _sut.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<DuplicateEmailException>()
                .WithMessage($"Email '{dto.Email}' is already registered.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnUserDto_WhenNewUserIsCreated()
        {
            // Arrange
            var dto = new CreateUserDto { Email = "new@example.com", Password = "Pass@123", FullName = "New User", Role = "JobSeeker" };
            var user = new User { UserId = Guid.NewGuid(), Email = dto.Email, FullName = dto.FullName, Role = dto.Role };
            var createdUserDto = new UserDto { UserId = user.UserId, Email = user.Email, FullName = user.FullName, Role = user.Role };

            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email))
                         .ReturnsAsync((User?)null);

            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                         .ReturnsAsync(user);

            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(createdUserDto);

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(dto.Email);
            result.FullName.Should().Be(dto.FullName);
            result.Role.Should().Be(dto.Role);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _sut.GetByIdAsync(userId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"User with id '{userId}' not found.");
        }

        // ---------- Login tests ----------

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var dto = new LoginDto { Email = "nouser@example.com", Password = "Pass@123" };

            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email))
                         .ReturnsAsync((User?)null);

            // Act
            var result = await _sut.LoginAsync(dto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsInvalid()
        {
            // Arrange
            var dto = new LoginDto { Email = "test@example.com", Password = "WrongPass" };
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass"),
                Role = "JobSeeker",
                FullName = "Tester"
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email))
                         .ReturnsAsync(user);

            // Act
            var result = await _sut.LoginAsync(dto);

            // Assert
            result.Should().BeNull();
        }
    }
}