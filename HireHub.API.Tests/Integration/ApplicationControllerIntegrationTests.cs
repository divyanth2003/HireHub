using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    public class ApplicationControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ApplicationControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // Seed a user/employer/job/jobseeker/resume + application; return relevant ids
        private (Guid userEmployerId, Guid userJobSeekerId, int jobId, int resumeId, int applicationId) SeedEverything()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();

            // create / ensure employer user
            var empUser = db.Users.FirstOrDefault(u => u.Email == "employer@hirehub.test");
            if (empUser == null)
            {
                empUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = "employer@hirehub.test",
                    FullName = "Employer Test",
                    PasswordHash = "pwd",
                    Role = "Employer",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),

                    Gender = "Other",
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(empUser);
                db.SaveChanges();
            }

            // create employer
            var employer = db.Employers.FirstOrDefault(e => e.UserId == empUser.UserId);
            if (employer == null)
            {
                employer = new Employer
                {
                    EmployerId = Guid.NewGuid(),
                    UserId = empUser.UserId,
                    CompanyName = "TestCo",
                    ContactInfo = "123"
                };
                db.Employers.Add(employer);
                db.SaveChanges();
            }

            // create job
            var job = new Job
            {
                EmployerId = employer.EmployerId,
                Title = "Integration Tester",
                Description = "Test job",
                CreatedAt = DateTime.UtcNow
            };
            db.Jobs.Add(job);
            db.SaveChanges();

            // create jobseeker user
            var jsUser = db.Users.FirstOrDefault(u => u.Email == "js@hirehub.test");
            if (jsUser == null)
            {
                jsUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = "js@hirehub.test",
                    FullName = "JobSeeker Test",
                    PasswordHash = "pwd",
                    Role = "JobSeeker",
                   DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),
                    Gender = "Other",
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(jsUser);
                db.SaveChanges();
            }

            // create jobseeker
            var js = db.JobSeekers.FirstOrDefault(s => s.UserId == jsUser.UserId);
            if (js == null)
            {
                js = new JobSeeker
                {
                    JobSeekerId = Guid.NewGuid(),
                    UserId = jsUser.UserId,
                    EducationDetails = "B.Tech",
                    Skills = "C#"
                };
                db.JobSeekers.Add(js);
                db.SaveChanges();
            }

            // create resume
            var resume = new Resume
            {
                JobSeekerId = js.JobSeekerId,
                ResumeName = "seed.pdf",
                FilePath = "/tmp/seed.pdf",
                UpdatedAt = DateTime.UtcNow,
                IsDefault = true
            };
            db.Resumes.Add(resume);
            db.SaveChanges();

            // create application
            var app = new Application
            {
                JobId = job.JobId,
                JobSeekerId = js.JobSeekerId,
                ResumeId = resume.ResumeId,
                CoverLetter = "Please hire me",
                Status = "Applied",
                AppliedAt = DateTime.UtcNow
            };
            db.Applications.Add(app);
            db.SaveChanges();

            return (empUser.UserId, jsUser.UserId, job.JobId, resume.ResumeId, app.ApplicationId);
        }

        [Fact]
        public async Task GetByJob_AsEmployer_ReturnsApplications()
        {
            // arrange
            var (empUserId, jsUserId, jobId, resumeId, appId) = SeedEverything();

            // client authenticated as Employer (must use employer's USER ID in auth header)
            var client = _factory.CreateClientWithTestAuth(role: "Employer", userId: empUserId);

            // act
            var resp = await client.GetAsync($"/api/application/job/{jobId}");

            // assert
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = await resp.Content.ReadFromJsonAsync<ApplicationDto[]>();
            list.Should().NotBeNull();
            list!.Any(a => a.ApplicationId == appId).Should().BeTrue();
        }

        [Fact]
        public async Task Create_AsJobSeeker_CreatesApplication_ReturnsCreated()
        {
            // arrange
            var (empUserId, jsUserId, jobId, resumeId, appId) = SeedEverything();

            // authenticate as jobseeker user (NameIdentifier should be jobseeker's user id)
            var client = _factory.CreateClientWithTestAuth(role: "JobSeeker", userId: jsUserId);

            var createDto = new CreateApplicationDto
            {
                JobId = jobId,
                JobSeekerId = Guid.NewGuid() // create new jobseeker id? better to use existing
            };

            // Actually use existing jobseeker id to avoid FK errors
            createDto.JobSeekerId = dbJobSeekerId(client, jsUserId);

            createDto.ResumeId = resumeId;
            createDto.CoverLetter = "Integration apply";

            // act
            var resp = await client.PostAsJsonAsync("/api/application", createDto);

            // assert
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await resp.Content.ReadFromJsonAsync<ApplicationDto>();
            created.Should().NotBeNull();
            created!.JobId.Should().Be(jobId);
            created.JobSeekerId.Should().Be(createDto.JobSeekerId);
        }

        // helper that reads jobseeker id from DB using factory's provider (by user id)
        private Guid dbJobSeekerId(HttpClient client, Guid userId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();
            var js = db.JobSeekers.FirstOrDefault(s => s.UserId == userId);
            if (js == null) throw new InvalidOperationException("JobSeeker not found for user");
            return js.JobSeekerId;
        }
    }
}
