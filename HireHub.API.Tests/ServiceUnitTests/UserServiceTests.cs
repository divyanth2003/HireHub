using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using AutoMapper;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HireHub.API.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Mock<IPasswordResetRepository> _passwordResetRepoMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<UserService>>();
            _passwordResetRepoMock = new Mock<IPasswordResetRepository>();
            _emailServiceMock = new Mock<IEmailService>();

            // Parameter order must match constructor:
            // (IUserRepository userRepository, IMapper mapper, ITokenService tokenService, ILogger<UserService> logger, IPasswordResetRepository passwordResetRepository, IEmailService emailService)
            _service = new UserService(
                _userRepoMock.Object,
                _mapperMock.Object,
                _tokenServiceMock.Object,
                _loggerMock.Object,
                _passwordResetRepoMock.Object,
                _emailServiceMock.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_UserExists_ReturnsUserDto()
        {
            var id = Guid.NewGuid();
            var user = new User { UserId = id, Email = "a@b.com", Role = "JobSeeker", PasswordHash = "h" };
            var expectedDto = new UserDto { UserId = id, Email = "a@b.com", Role = "JobSeeker" };

            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(expectedDto);

            var result = await _service.GetByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal(expectedDto.UserId, result.UserId);
            Assert.Equal(expectedDto.Email, result.Email);
        }

        [Fact]
        public async Task GetByIdAsync_UserNotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));
        }

        [Fact]
        public async Task CreateAsync_DuplicateEmail_ThrowsDuplicateEmailException()
        {
            var dto = new CreateUserDto
            {
                Email = "dupe@a.com",
                Password = "pass123",
                FullName = "N",
                Role = "JobSeeker",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)),
                Gender = "M"
            };

            _userRepoMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(true);

            await Assert.ThrowsAsync<DuplicateEmailException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedUserDto()
        {
            var dto = new CreateUserDto
            {
                Email = "new@a.com",
                Password = "pass123",
                FullName = "New User",
                Role = "JobSeeker",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-22)),
                Gender = "F",
                Address = "addr"
            };

            var createdUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = dto.Email,
                Role = dto.Role,
                PasswordHash = "hashed"
            };

            var createdDto = new UserDto
            {
                UserId = createdUser.UserId,
                Email = createdUser.Email,
                Role = createdUser.Role
            };

            _userRepoMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
            // mapper should create a new User instance that the service will populate
            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(new User());
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(createdUser);
            _mapperMock.Setup(m => m.Map<UserDto>(createdUser)).Returns(createdDto);

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(createdDto.UserId, result.UserId);
            Assert.Equal(createdDto.Email, result.Email);
        }

        [Fact]
        public async Task LoginAsync_InvalidEmail_ReturnsNull()
        {
            var dto = new LoginDto { Email = "no@a.com", Password = "pwd" };
            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);

            var result = await _service.LoginAsync(dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
        {
            var dto = new LoginDto { Email = "a@b.com", Password = "wrong" };
            var user = new User { UserId = Guid.NewGuid(), Email = dto.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword("right") };

            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);

            var result = await _service.LoginAsync(dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
        {
            var password = "myPass1!";
            var dto = new LoginDto { Email = "valid@a.com", Password = password };
            var user = new User { UserId = Guid.NewGuid(), Email = dto.Email, Role = "Employer", PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };

            var jwtToken = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(1),
                claims: null);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.CreateToken(user.UserId, user.Role, user.Email)).Returns(tokenString);

            var result = await _service.LoginAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(user.Role, result.Role);
            Assert.Equal(user.UserId, result.UserId);
            Assert.Equal(tokenString, result.Token);
            Assert.True(result.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task UpdateAsync_UserExists_ReturnsUpdatedUserDto()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateUserDto
            {
                FullName = "Updated User",
                Role = "Employer",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                Gender = "F",
                Address = "New Address"
            };

            var existingUser = new User { UserId = id, Email = "user@a.com" };
            var updatedUser = new User { UserId = id, Email = "user@a.com", Address = dto.Address };
            var expectedDto = new UserDto { UserId = id, Email = "user@a.com", Address = dto.Address };

            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingUser);
            // when AutoMapper maps into the existing user, return the same instance
            _mapperMock.Setup(m => m.Map(dto, existingUser)).Returns(existingUser);
            _userRepoMock.Setup(r => r.UpdateAsync(existingUser)).ReturnsAsync(updatedUser);
            _mapperMock.Setup(m => m.Map<UserDto>(updatedUser)).Returns(expectedDto);

            var result = await _service.UpdateAsync(id, dto);

            Assert.NotNull(result);
            Assert.Equal(expectedDto.UserId, result.UserId);
            Assert.Equal(expectedDto.Address, result.Address);
        }

        [Fact]
        public async Task UpdateAsync_UserNotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateUserDto
            {
                FullName = "Not Exist",
                Role = "JobSeeker",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-22)),
                Gender = "M",
                Address = "Nowhere"
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task DeleteAsync_UserExists_ReturnsTrue()
        {
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(id);

            Assert.True(result);
            _userRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_UserNotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
        }

        // ----------------- New tests for new service methods -----------------

        [Fact]
        public async Task DeletePermanentlyAsync_WhenRepositoryReturnsTrue_ReturnsTrue()
        {
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.DeletePermanentlyAsync(id)).ReturnsAsync(true);

            var res = await _service.DeletePermanentlyAsync(id);

            Assert.True(res);
            _userRepoMock.Verify(r => r.DeletePermanentlyAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeletePermanentlyAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
        {
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.DeletePermanentlyAsync(id)).ReturnsAsync(false);

            var res = await _service.DeletePermanentlyAsync(id);

            Assert.False(res);
            _userRepoMock.Verify(r => r.DeletePermanentlyAsync(id), Times.Once);
        }

        [Fact]
        public async Task ScheduleDeletionAsync_UserExists_ReturnsMessageAndCallsRepo()
        {
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, Email = "u@a.com" };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.ScheduleDeletionAsync(userId, It.IsAny<DateTime>())).Returns(Task.CompletedTask);

            var message = await _service.ScheduleDeletionAsync(userId, 7);

            Assert.Contains("Account scheduled for deletion", message);
            _userRepoMock.Verify(r => r.ScheduleDeletionAsync(userId, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleDeletionAsync_UserNotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.ScheduleDeletionAsync(userId, 10));
        }

        [Fact]
        public async Task DeactivateAsync_UserExists_ReturnsConfirmationAndCallsRepo()
        {
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, Email = "u@a.com" };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.DeactivateAsync(userId)).Returns(Task.CompletedTask);

            var res = await _service.DeactivateAsync(userId);

            Assert.Equal("Account deactivated.", res);
            _userRepoMock.Verify(r => r.DeactivateAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DeactivateAsync_UserNotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeactivateAsync(userId));
        }
    }
}
