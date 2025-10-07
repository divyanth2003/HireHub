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
        private readonly UserService _service;

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<UserService>>();

            _service = new UserService(
                _userRepoMock.Object,
                _mapperMock.Object,
                _tokenServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_UserExists_ReturnsUserDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = new User { UserId = id, Email = "a@b.com", Role = "JobSeeker", PasswordHash = "h" };
            var expectedDto = new UserDto { UserId = id, Email = "a@b.com", Role = "JobSeeker" };

            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(expectedDto);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.UserId, result.UserId);
            Assert.Equal(expectedDto.Email, result.Email);
        }

        [Fact]
        public async Task GetByIdAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));
        }

        [Fact]
        public async Task CreateAsync_DuplicateEmail_ThrowsDuplicateEmailException()
        {
            // Arrange
            var dto = new CreateUserDto { Email = "dupe@a.com", Password = "pass123", FullName = "N", Role = "JobSeeker", DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)), Gender = "M" };
            _userRepoMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<DuplicateEmailException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedUserDto()
        {
            // Arrange
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
            // mapper will map CreateUserDto -> User (for Add) and User -> UserDto (for return)
            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(new User()); // intermediate user
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(createdUser);
            _mapperMock.Setup(m => m.Map<UserDto>(createdUser)).Returns(createdDto);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdDto.UserId, result.UserId);
            Assert.Equal(createdDto.Email, result.Email);
        }

        [Fact]
        public async Task LoginAsync_InvalidEmail_ReturnsNull()
        {
            // Arrange
            var dto = new LoginDto { Email = "no@a.com", Password = "pwd" };
            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.LoginAsync(dto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
        {
            // Arrange
            var dto = new LoginDto { Email = "a@b.com", Password = "wrong" };
            var user = new User { UserId = Guid.NewGuid(), Email = dto.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword("right") };

            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);

            // Act
            var result = await _service.LoginAsync(dto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var password = "myPass1!";
            var dto = new LoginDto { Email = "valid@a.com", Password = password };
            var user = new User { UserId = Guid.NewGuid(), Email = dto.Email, Role = "Employer", PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };

            // create a simple JWT string (unsigned) with expiry so JwtSecurityTokenHandler.ReadJwtToken works
            var jwtToken = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(1),
                claims: null);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.CreateToken(user.UserId, user.Role, user.Email)).Returns(tokenString);

            // Act
            var result = await _service.LoginAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Role, result.Role);
            Assert.Equal(user.UserId, result.UserId);
            Assert.Equal(tokenString, result.Token);
            Assert.True(result.ExpiresAt > DateTime.UtcNow);
        }
        // ------------------- UPDATE TESTS -------------------
        [Fact]
        public async Task UpdateAsync_UserExists_ReturnsUpdatedUserDto()
        {
            // Arrange
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
            _mapperMock.Setup(m => m.Map(dto, existingUser));
            _userRepoMock.Setup(r => r.UpdateAsync(existingUser)).ReturnsAsync(updatedUser);
            _mapperMock.Setup(m => m.Map<UserDto>(updatedUser)).Returns(expectedDto);

            // Act
            var result = await _service.UpdateAsync(id, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.UserId, result.UserId);
            Assert.Equal(expectedDto.Address, result.Address);
        }

        [Fact]
        public async Task UpdateAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
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

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(id, dto));
        }

        // ------------------- DELETE TESTS -------------------
        [Fact]
        public async Task DeleteAsync_UserExists_ReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            Assert.True(result);
            _userRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
        }

    }
}
