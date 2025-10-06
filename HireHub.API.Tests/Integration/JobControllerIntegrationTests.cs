using System;
using System.Linq;
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
    public class JobControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public JobControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetById_ShouldReturn_SeededJob()
        {
            // arrange - seed employer + job
            int jobId;
            var employerId = Guid.NewGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();

                db.Employers.Add(new Employer
                {
                    EmployerId = employerId,
                    CompanyName = "TestCo",
                    UserId = _factory.SeedAdminUserId
                });

                var job = new Job
                {
                    JobId = 0, // if identity not used in memory you can set explicit; set after SaveChanges if auto increment
                    EmployerId = employerId,
                    Title = "Integration Test Job",
                    Description = "Desc",
                    Location = "Remote",
                    SkillsRequired = "C#",
                    CreatedAt = DateTime.UtcNow
                };

                db.Jobs.Add(job);
                db.SaveChanges();

                // obtain actual id (Db may assign)
                jobId = job.JobId;
            }

            // act
            var client = _factory.CreateClientWithTestAuth(role: "Admin");
            var resp = await client.GetAsync($"/api/job/{jobId}");

            // assert
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<JobDto>();
            dto.Should().NotBeNull();
            dto!.JobId.Should().Be(jobId);
            dto.Title.Should().Be("Integration Test Job");
            dto.EmployerId.Should().Be(employerId);
        }

        [Fact]
        public async Task GetByEmployer_AsEmployer_ShouldReturnJobs()
        {
            // arrange
            var employerId = Guid.NewGuid();
            var jobGuidTitle = "EmployerJob";

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();

                db.Employers.Add(new Employer
                {
                    EmployerId = employerId,
                    CompanyName = "CompanyX",
                    UserId = _factory.SeedAdminUserId
                });

                db.Jobs.AddRange(
                    new Job { EmployerId = employerId, Title = jobGuidTitle, Description = "d", CreatedAt = DateTime.UtcNow },
                    new Job { EmployerId = employerId, Title = "Other", Description = "d", CreatedAt = DateTime.UtcNow }
                );

                db.SaveChanges();
            }

            // call as Employer role (owner role)
            var client = _factory.CreateClientWithTestAuth(role: "Employer");
            // header will contain seeded admin id by default; employer authorization isn't actually checked here beyond role
            var resp = await client.GetAsync($"/api/job/employer/{employerId}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = await resp.Content.ReadFromJsonAsync<JobDto[]>();
            list.Should().NotBeNull();
            list!.Any(j => j.Title == jobGuidTitle).Should().BeTrue();
        }
    }
}
