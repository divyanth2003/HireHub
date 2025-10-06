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
    public class ResumeControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ResumeControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // helper: seed a jobseeker + resume and return tuple (jobSeekerId, resumeId)
        private (Guid jobSeekerId, int resumeId) SeedResumeInDb()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();

            // create a user (if not present)
            var user = db.Users.FirstOrDefault(u => u.Email == "js@hirehub.test");
            if (user == null)
            {
                user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = "js@hirehub.test",
                    FullName = "JS Test",
                    PasswordHash = "hash",
                    Role = "JobSeeker",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),

                    Gender = "Other",
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(user);
                db.SaveChanges();
            }

            // add jobseeker (model assumed to exist)
            var jobSeeker = db.Set<JobSeeker>().FirstOrDefault(js => js.UserId == user.UserId);
            if (jobSeeker == null)
            {
                jobSeeker = new JobSeeker
                {
                    JobSeekerId = Guid.NewGuid(),
                    UserId = user.UserId,
                    EducationDetails = "BSc",
                    Skills = "C#",
                    College = "TestCollege"
                };
                db.JobSeekers.Add(jobSeeker);
                db.SaveChanges();
            }

            // add resume
            var resume = new Resume
            {
                // assuming Resume.ResumeId is identity (int). If not identity, change accordingly.
                ResumeName = "Test Resume",
                FilePath = "/tmp/resume.pdf",
                JobSeekerId = jobSeeker.JobSeekerId,
                IsDefault = true,
                UpdatedAt = DateTime.UtcNow
            };
            db.Resumes.Add(resume);
            db.SaveChanges();

            return (jobSeeker.JobSeekerId, resume.ResumeId);
        }

        [Fact]
        public async Task GetByJobSeeker_AsJobSeekerOrAdmin_ReturnsResumes()
        {
            // arrange
            var (jobSeekerId, resumeId) = SeedResumeInDb();

            // client as JobSeeker (test auth)
            var client = _factory.CreateClientWithTestAuth(role: "JobSeeker", userId: jobSeekerId);

            // act
            var resp = await client.GetAsync($"/api/resume/jobseeker/{jobSeekerId}");

            // assert
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = await resp.Content.ReadFromJsonAsync<ResumeDto[]>();
            list.Should().NotBeNull();
            list!.Any(r => r.ResumeId == resumeId).Should().BeTrue();
        }

        [Fact]
        public async Task Create_AsJobSeeker_CreatesResume_ReturnsCreated()
        {
            // arrange: ensure jobseeker in DB
            var (jobSeekerId, _) = SeedResumeInDb();

            var client = _factory.CreateClientWithTestAuth(role: "JobSeeker", userId: jobSeekerId);

            var createDto = new CreateResumeDto
            {
                JobSeekerId = jobSeekerId,
                ResumeName = "NewResume",
                FilePath = "/tmp/new.pdf",
                ParsedSkills = "C#,SQL",
                FileType = "pdf",
                IsDefault = false
            };

            // act
            var resp = await client.PostAsJsonAsync("/api/resume", createDto);

            // assert
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await resp.Content.ReadFromJsonAsync<ResumeDto>();
            created.Should().NotBeNull();
            created!.ResumeName.Should().Be(createDto.ResumeName);
            created.JobSeekerId.Should().Be(jobSeekerId);
        }
    }
}
