using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HireHub.API.Tests.Integration
{
    public class JobSeekerControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public JobSeekerControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetByUserId_AsAdmin_ShouldReturn_SeededJobSeeker()
        {
            // arrange - seed a JobSeeker linked to the seeded admin user
            var seededJsId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();

                // ensure unique - optional cleanup (InMemory DB already reset per factory config)
                var js = new JobSeeker
                {
                    JobSeekerId = seededJsId,
                    UserId = _factory.SeedAdminUserId,
                    EducationDetails = "BS CS",
                    Skills = "C#,SQL",
                    College = "TestU",
                    WorkStatus = "Open",
                    Experience = "2 years"
                };

                db.JobSeekers.Add(js);
                db.SaveChanges();
            }

            // act - call GET by admin
            var client = _factory.CreateClientWithTestAuth(role: "Admin");
            var resp = await client.GetAsync($"/api/jobseeker/by-user/{_factory.SeedAdminUserId}");

            // assert
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<JobSeekerDto>();
            dto.Should().NotBeNull();
            dto!.JobSeekerId.Should().Be(seededJsId);
            dto.UserId.Should().Be(_factory.SeedAdminUserId);
            dto.College.Should().Be("TestU");
        }

        [Fact]
        public async Task GetByUserId_AsOwner_ShouldReturn_JobSeeker()
        {
            // arrange - seed a new user & jobseeker pair
            var userId = Guid.NewGuid();
            var jsId = Guid.NewGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();

                db.Users.Add(new User
                {
                    UserId = userId,
                    FullName = "Owner",
                    Email = $"owner+{Guid.NewGuid()}@test",
                    PasswordHash = "x",
                    Role = "JobSeeker",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),

                    Gender = "Other",
                    CreatedAt = DateTime.UtcNow
                });

                db.JobSeekers.Add(new JobSeeker
                {
                    JobSeekerId = jsId,
                    UserId = userId,
                    College = "OwnerCollege",
                    Skills = "X",
                });

                db.SaveChanges();
            }

            // act - call GET as owner
            var client = _factory.CreateClientWithTestAuth(role: "JobSeeker", userId: userId);
            var resp = await client.GetAsync($"/api/jobseeker/by-user/{userId}");

            // assert
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<JobSeekerDto>();
            dto.Should().NotBeNull();
            dto!.UserId.Should().Be(userId);
            dto.JobSeekerId.Should().Be(jsId);
            dto.College.Should().Be("OwnerCollege");
        }
    }
}
