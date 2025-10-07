using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using HireHub.API.Controllers;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HireHub.API.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _userService;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            // Arrange: mock all dependencies needed by UserService
            _userRepoMock = new Mock<IUserRepository>();
            _mapperMock = new Mock<IMapper>();
            _tokenServiceMock = new Mock<ITokenService>();
            _loggerMock = new Mock<ILogger<UserService>>();

            // Create real instance of UserService (with mocked dependencies)
            _userService = new UserService(
                _userRepoMock.Object,
                _mapperMock.Object,
                _tokenServiceMock.Object,
                _loggerMock.Object
            );

            // Inject that instance into controller
            _controller = new UserController(_userService);
        }

        // ------------------- REGISTER -------------------
        [Fact]
        public async Task Register_ValidDto_ReturnsCreatedAtAction()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                FullName = "Test User",
                Email = "test@a.com",
                Password = "123456",
                Role = "JobSeeker",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                Gender = "M",
                Address = "Somewhere"
            };

            var createdUser = new User
            {
                UserId = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = dto.Email,
                Role = dto.Role
            };

            var createdDto = new UserDto
            {
                UserId = createdUser.UserId,
                FullName = createdUser.FullName,
                Email = createdUser.Email,
                Role = createdUser.Role
            };

            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(createdUser);
            _userRepoMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(createdUser);
            _mapperMock.Setup(m => m.Map<UserDto>(createdUser)).Returns(createdDto);

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var createdAt = Assert.IsType<CreatedAtActionResult>(result);
            var returnedDto = Assert.IsType<UserDto>(createdAt.Value);
            Assert.Equal(dto.Email, returnedDto.Email);
        }

        // ------------------- LOGIN -------------------
        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var dto = new LoginDto { Email = "wrong@a.com", Password = "badpass" };
            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.Login(dto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithAuthResponse()
        {
            // Arrange
            var dto = new LoginDto { Email = "ok@a.com", Password = "password" };
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Employer"
            };

            // ✅ Use a valid JWT format token
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9."
                       + "eyJzdWIiOiIxMjM0NTY3ODkwIiwiZW1haWwiOiJ0ZXN0QHRlc3QuY29tIiwicm9sZSI6IkpvYlNlZWtlciIsImV4cCI6MTk5OTk5OTk5OX0."
                       + "abc123signature";

            _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.CreateToken(user.UserId, user.Role, user.Email)).Returns(token);

            // Act
            var result = await _controller.Login(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var authResponse = Assert.IsType<AuthResponseDto>(ok.Value);
            Assert.Equal(token, authResponse.Token);
            Assert.Equal(user.Role, authResponse.Role);
        }


        // ------------------- GET -------------------
        [Fact]
        public async Task GetAll_ReturnsOkResultWithUsers()
        {
            // Arrange
            var users = new List<User> { new User { UserId = Guid.NewGuid(), Email = "a@b.com" } };
            var userDtos = new List<UserDto> { new UserDto { UserId = users[0].UserId, Email = users[0].Email } };

            _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(userDtos);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<UserDto>>(ok.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetById_ExistingUser_ReturnsOkWithUserDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = new User { UserId = id, Email = "u@a.com" };
            var dto = new UserDto { UserId = id, Email = "u@a.com" };

            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(dto);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<UserDto>(ok.Value);
            Assert.Equal(dto.Email, value.Email);
        }

        // ------------------- UPDATE -------------------
        [Fact]
        public async Task Update_ValidUser_ReturnsOkWithUpdatedDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateUserDto
            {
                FullName = "Updated",
                Role = "Employer",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                Gender = "M",
                Address = "New Addr"
            };

            var existingUser = new User { UserId = id, Email = "u@a.com" };
            var updatedUser = new User { UserId = id, Email = "u@a.com", Address = "New Addr" };
            var updatedDto = new UserDto { UserId = id, Email = "u@a.com", Address = "New Addr" };

            _userRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingUser);
            _mapperMock.Setup(m => m.Map(dto, existingUser));
            _userRepoMock.Setup(r => r.UpdateAsync(existingUser)).ReturnsAsync(updatedUser);
            _mapperMock.Setup(m => m.Map<UserDto>(updatedUser)).Returns(updatedDto);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<UserDto>(ok.Value);
            Assert.Equal("New Addr", returned.Address);
        }

        // ------------------- DELETE -------------------
        [Fact]
        public async Task Delete_ExistingUser_ReturnsNoContent()
        {
            // Arrange
            var id = Guid.NewGuid();
            _userRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
    }
}
