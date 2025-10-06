using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using HireHub.API.DTOs;
using Xunit;

namespace HireHub.API.Tests.Integration
{
    public class UserControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UserControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAll_ShouldRequireAdminRole()
        {
            // arrange
            var anonClient = _factory.CreateClient(); // no test auth headers -> not authorized
            var nonAdminClient = _factory.CreateClientWithTestAuth(role: "JobSeeker"); // non-admin

            // act - anonymous
            var anonResp = await anonClient.GetAsync("/api/User");
            // act - non-admin
            var nonAdminResp = await nonAdminClient.GetAsync("/api/User");

            // assert - anonymous should be 401 (unauthenticated) or NoResult
            // ASP.NET by default returns 401 when auth required and no credentials are present
            anonResp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);

            // assert - non-admin authenticated but not in Admin role -> Forbidden
            nonAdminResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // act - admin
            var adminClient = _factory.CreateClientWithTestAuth(role: "Admin", userId: _factory.SeedAdminUserId);
            var adminResp = await adminClient.GetAsync("/api/User");

            // assert - admin should get 200 OK
            adminResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // optionally verify returned list structure
            var users = await adminResp.Content.ReadFromJsonAsync<UserDto[]>();
            users.Should().NotBeNull();
            users.Should().Contain(u => u.Email == _factory.SeedAdminEmail);
        }

        [Fact]
        public async Task Admin_CanGetUserById()
        {
            // arrange admin client (uses seeded admin created by CustomWebApplicationFactory)
            var adminClient = _factory.CreateClientWithTestAuth(role: "Admin", userId: _factory.SeedAdminUserId);

            // act
            var resp = await adminClient.GetAsync($"/api/User/{_factory.SeedAdminUserId}");

            // assert
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<UserDto>();
            dto.Should().NotBeNull();
            dto!.UserId.Should().Be(_factory.SeedAdminUserId);
            dto.Email.Should().Be(_factory.SeedAdminEmail);
        }

        [Fact]
        public async Task Register_Then_Login_Should_Return_AuthResponse()
        {
            // arrange - client without auth for register/login
            var client = _factory.CreateClient();
            var createDto = new CreateUserDto
            {
                FullName = "IT Test User",
                Email = $"testuser.{Guid.NewGuid():N}@hirehub.test",
                Password = "Password1!",
                Role = "JobSeeker",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),
                Gender = "Other",
                Address = "Test Address"
            };

            // act - register
            var registerResp = await client.PostAsJsonAsync("/api/User/register", createDto);

            // assert register
            registerResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await registerResp.Content.ReadFromJsonAsync<UserDto>();
            created.Should().NotBeNull();
            created!.Email.Should().Be(createDto.Email);

            // act - login
            var login = new LoginDto { Email = createDto.Email, Password = createDto.Password };
            var loginResp = await client.PostAsJsonAsync("/api/User/login", login);

            // assert login
            loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponseDto>();
            auth.Should().NotBeNull();
            auth!.UserId.Should().NotBeEmpty();
            auth.Role.Should().Be(createDto.Role);
            auth.Token.Should().NotBeNullOrWhiteSpace();
        }
    }
}
