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
    public class EmployerControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EmployerControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAll_ShouldRequireAdminRole()
        {
            var anonClient = _factory.CreateClient();
            var nonAdminClient = _factory.CreateClientWithTestAuth(role: "JobSeeker");
            var adminClient = _factory.CreateClientWithTestAuth(role: "Admin", userId: _factory.SeedAdminUserId);

            var anonResp = await anonClient.GetAsync("/api/Employer");
            var nonAdminResp = await nonAdminClient.GetAsync("/api/Employer");
            var adminResp = await adminClient.GetAsync("/api/Employer");

            anonResp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
            nonAdminResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            adminResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await adminResp.Content.ReadFromJsonAsync<EmployerDto[]>();
            list.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_Then_GetById_AsAdmin()
        {
            var adminClient = _factory.CreateClientWithTestAuth(role: "Admin", userId: _factory.SeedAdminUserId);

            var createDto = new CreateEmployerDto
            {
                UserId = _factory.SeedAdminUserId,
                CompanyName = $"IntegrationCo {Guid.NewGuid():N}",
                ContactInfo = "contact@test",
                Position = "CTO"
            };

            var createResp = await adminClient.PostAsJsonAsync("/api/Employer", createDto);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);

            var created = await createResp.Content.ReadFromJsonAsync<EmployerDto>();
            created.Should().NotBeNull();
            created!.CompanyName.Should().Be(createDto.CompanyName);
            created.UserId.Should().Be(createDto.UserId);

            var getResp = await adminClient.GetAsync($"/api/Employer/{created.EmployerId}");
            getResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var got = await getResp.Content.ReadFromJsonAsync<EmployerDto>();
            got.Should().NotBeNull();
            got!.EmployerId.Should().Be(created.EmployerId);
            got.CompanyName.Should().Be(createDto.CompanyName);
        }

        [Fact]
        public async Task SearchByCompany_ShouldReturnResults_Anonymous()
        {
            var adminClient = _factory.CreateClientWithTestAuth(role: "Admin", userId: _factory.SeedAdminUserId);
            var anonClient = _factory.CreateClient();

            var uniqueName = $"SearchCo_{Guid.NewGuid():N}";
            var createDto = new CreateEmployerDto
            {
                UserId = _factory.SeedAdminUserId,
                CompanyName = uniqueName,
                ContactInfo = "search-contact",
                Position = "Manager"
            };

            var createResp = await adminClient.PostAsJsonAsync("/api/Employer", createDto);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);

            var searchResp = await anonClient.GetAsync($"/api/Employer/search?company={Uri.EscapeDataString(uniqueName)}");
            searchResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = await searchResp.Content.ReadFromJsonAsync<EmployerDto[]>();
            results.Should().NotBeNull();
            results!.Should().Contain(e => string.Equals(e.CompanyName, uniqueName, StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public async Task Update_AsAdmin_ShouldReturnUpdated()
        {
            var adminClient = _factory.CreateClientWithTestAuth(role: "Admin", userId: _factory.SeedAdminUserId);

            var createDto = new CreateEmployerDto
            {
                UserId = _factory.SeedAdminUserId,
                CompanyName = $"ToUpdateCo_{Guid.NewGuid():N}",
                ContactInfo = "before@co",
                Position = "Dev"
            };
            var createResp = await adminClient.PostAsJsonAsync("/api/Employer", createDto);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResp.Content.ReadFromJsonAsync<EmployerDto>();
            created.Should().NotBeNull();

            var updateDto = new UpdateEmployerDto
            {
                CompanyName = created!.CompanyName + " - Updated",
                ContactInfo = "after@co",
                Position = "CTO"
            };

            var putResp = await adminClient.PutAsJsonAsync($"/api/Employer/{created.EmployerId}", updateDto);
            putResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await putResp.Content.ReadFromJsonAsync<EmployerDto>();
            updated.Should().NotBeNull();
            updated!.CompanyName.Should().Be(updateDto.CompanyName);
            updated.ContactInfo.Should().Be(updateDto.ContactInfo);
            updated.Position.Should().Be(updateDto.Position);
        }

        [Fact]
        public async Task Delete_AsAdmin_ShouldRemoveEmployer()
        {
            var adminClient = _factory.CreateClientWithTestAuth(role: "Admin", userId: _factory.SeedAdminUserId);

            var createDto = new CreateEmployerDto
            {
                UserId = _factory.SeedAdminUserId,
                CompanyName = $"ToDeleteCo_{Guid.NewGuid():N}",
                ContactInfo = "delete@co",
                Position = "Temp"
            };

            var createResp = await adminClient.PostAsJsonAsync("/api/Employer", createDto);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResp.Content.ReadFromJsonAsync<EmployerDto>();
            created.Should().NotBeNull();

            var delResp = await adminClient.DeleteAsync($"/api/Employer/{created!.EmployerId}");
            delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // subsequent GET should be NotFound (assuming NotFoundException => 404 mapping)
            var getAfterDel = await adminClient.GetAsync($"/api/Employer/{created.EmployerId}");
            getAfterDel.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
