// HireHub.API.Tests.Unit/UserServiceTests.cs
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using HireHub.API.Services;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Models;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;

namespace HireHub.API.Tests.Unit
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITokenService> _tokenMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _repoMock = new Mock<IUserRepository>();
            _mapperMock = new Mock<IMapper>();
            _tokenMock = new Mock<ITokenService>();
            _loggerMock = new Mock<ILogger<UserService>>();

            _sut = new UserService(
                _repoMock.Object,
                _mapperMock.Object,
                _tokenMock.Object,
                _loggerMock.Object
            );
        }

        // ---------------- helpers ----------------

        private static User MakeUser(Guid id, string email = "a@b.test", string role = "JobSeeker", string pwd = "Password1!")
        {
            return new User
            {
                UserId = id,
                FullName = "Test User",
                Email = email,
                // create a real bcrypt hash so BCrypt.Verify works if your service uses it
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd),
                Role = role,
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),

                Gender = "Other",
                Address = "Addr",
                CreatedAt = DateTime.UtcNow
            };
        }

        private static CreateUserDto MakeCreateDto(string email = "n@t.test")
        {
            return new CreateUserDto
            {
                FullName = "New User",
                Email = email,
                Password = "Password1!",
                Role = "JobSeeker",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),

                Gender = "Other",
                Address = "Addr"
            };
        }

        // helper to create a signed JWT string (only to be parsed by JwtSecurityTokenHandler.ReadJwtToken)
        private static string BuildJwtTokenString(DateTime expires)
        {
            // We only need a well-formed JWT string (the handler ReadJwtToken will parse it).
            // This signing key is only for token formatting (no validation in tests).
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("tests-are-fun-and-need-a-key-32chars!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: "TestIssuer",
                audience: "TestAudience",
                claims: null,
                notBefore: DateTime.UtcNow.AddMinutes(-5),
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        // ---------------- tests ----------------

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedList()
        {
            var users = new List<User>
            {
                MakeUser(Guid.NewGuid(), "a@test"),
                MakeUser(Guid.NewGuid(), "b@test")
            };

            var dtos = new List<UserDto>
            {
                new UserDto { UserId = users[0].UserId, Email = users[0].Email },
                new UserDto { UserId = users[1].UserId, Email = users[1].Email }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(dtos);

            var result = await _sut.GetAllAsync();

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFound_WhenMissing()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((User?)null);

            Func<Task> act = async () => await _sut.GetByIdAsync(id);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"User with id '{id}' not found.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnMappedDto_WhenCreated()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "Password123",
                Role = "JobSeeker",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),

                Gender = "Other",
                Address = "Somewhere"
            };

            var entity = new User
            {
                UserId = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = "hashed-password",
                Role = dto.Role,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Address = dto.Address,
                CreatedAt = DateTime.UtcNow
            };

            // ✅ Mock mapping from DTO → User
            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(entity);

            // ✅ Mock repo to return the same entity
            _repoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(entity);

            // ✅ Mock mapping from User → UserDto
            _mapperMock.Setup(m => m.Map<UserDto>(entity))
                       .Returns(new UserDto
                       {
                           UserId = entity.UserId,
                           FullName = entity.FullName,
                           Email = entity.Email,
                           Role = entity.Role,
                           DateOfBirth = entity.DateOfBirth,
                           Gender = entity.Gender,
                           Address = entity.Address
                       });

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(dto.Email);
            result.FullName.Should().Be(dto.FullName);
            result.Role.Should().Be(dto.Role);
            result.UserId.Should().NotBeEmpty(); // just check it's created
        }



        [Fact]
        public async Task CreateAsync_ShouldThrowDuplicate_WhenEmailExists()
        {
            var createDto = MakeCreateDto("exists@test");
            _repoMock.Setup(r => r.ExistsByEmailAsync(createDto.Email)).ReturnsAsync(true);

            Func<Task> act = async () => await _sut.CreateAsync(createDto);

            await act.Should().ThrowAsync<DuplicateEmailException>();
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFound_WhenMissing()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateUserDto
            {
                FullName = "X",
                Role = "JobSeeker",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),

                Gender = "Other",
                Address = "A"
            };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((User?)null);

            Func<Task> act = async () => await _sut.UpdateAsync(id, dto);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage($"User with id '{id}' not found.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnMappedDto_WhenUpdated()
        {
            var id = Guid.NewGuid();
            var existing = MakeUser(id, "u@test");
            var updatedEntity = new User
            {
                UserId = id,
                FullName = existing.FullName,
                Email = existing.Email,
                PasswordHash = existing.PasswordHash,
                Role = "Employer",
                DateOfBirth = existing.DateOfBirth,
                Gender = existing.Gender,
                Address = existing.Address
            };

            var updateDto = new UpdateUserDto
            {
                FullName = updatedEntity.FullName,
                Role = updatedEntity.Role,
                DateOfBirth = updatedEntity.DateOfBirth,
                Gender = updatedEntity.Gender,
                Address = updatedEntity.Address
            };

            var returnedDto = new UserDto
            {
                UserId = id,
                FullName = updatedEntity.FullName,
                Email = updatedEntity.Email,
                Role = updatedEntity.Role,
                DateOfBirth = updatedEntity.DateOfBirth,
                Gender = updatedEntity.Gender,
                Address = updatedEntity.Address
            };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.UpdateAsync(It.Is<User>(u => u.UserId == id))).ReturnsAsync(updatedEntity);
            _mapperMock.Setup(m => m.Map<UserDto>(updatedEntity)).Returns(returnedDto);

            var result = await _sut.UpdateAsync(id, updateDto);

            result.Should().NotBeNull();
            result.Role.Should().Be(updateDto.Role);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFound_WhenFalse()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            Func<Task> act = async () => await _sut.DeleteAsync(id);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage($"User with id '{id}' not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenDeleted()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _sut.DeleteAsync(id);

            result.Should().BeTrue();
        }

        // ---------- Login tests ----------
        [Fact]
        public async Task LoginAsync_ShouldReturnAuthResponse_WhenValidCredentials()
        {
            var password = "Password1!";
            var user = MakeUser(Guid.NewGuid(), "auth@test", role: "JobSeeker", pwd: password);

            // Build a valid JWT string with a future expiry so UserService's JwtSecurityTokenHandler.ReadJwtToken can parse it.
            var tokenString = BuildJwtTokenString(DateTime.UtcNow.AddHours(1));

            // repo returns the user
            _repoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

            // token service returns a well-formed JWT
            _tokenMock.Setup(t => t.CreateToken(user.UserId, user.Role, user.Email))
                      .Returns(tokenString);

            // call
            var dto = new LoginDto { Email = user.Email, Password = password };

            var result = await _sut.LoginAsync(dto);

            result.Should().NotBeNull();
            result!.Token.Should().Be(tokenString);
            result.UserId.Should().Be(user.UserId);
            result.Role.Should().Be(user.Role);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenInvalidCredentials()
        {
            var user = MakeUser(Guid.NewGuid(), "bad@test", pwd: "RightPass!");
            _repoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

            var dto = new LoginDto { Email = user.Email, Password = "Wrong!" };

            var result = await _sut.LoginAsync(dto);

            result.Should().BeNull();
        }
    }
}
