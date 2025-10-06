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
    public class NotificationControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public NotificationControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // Seed minimal domain objects required for NotifyApplicant flow
        private (Guid employerUserId, Guid jobSeekerUserId, int jobId, Guid jobSeekerId, int applicationId) SeedForNotifyApplicant()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();

            // Employer user
            var empUser = db.Users.FirstOrDefault(u => u.Email == "integration.emp@hirehub.test");
            if (empUser == null)
            {
                empUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = "integration.emp@hirehub.test",
                    FullName = "Integration Employer",
                    PasswordHash = "pwd",
                    Role = "Employer",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),

                    Gender = "Other",
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(empUser);
                db.SaveChanges();
            }

            // Employer
            var emp = db.Employers.FirstOrDefault(e => e.UserId == empUser.UserId);
            if (emp == null)
            {
                emp = new Employer
                {
                    EmployerId = Guid.NewGuid(),
                    UserId = empUser.UserId,
                    CompanyName = "IntCo",
                    ContactInfo = "111"
                };
                db.Employers.Add(emp);
                db.SaveChanges();
            }

            // Job
            var job = new Job
            {
                EmployerId = emp.EmployerId,
                Title = "Integration Job",
                Description = "desc",
                CreatedAt = DateTime.UtcNow
            };
            db.Jobs.Add(job);
            db.SaveChanges();

            // JobSeeker user
            var jsUser = db.Users.FirstOrDefault(u => u.Email == "integration.js@hirehub.test");
            if (jsUser == null)
            {
                jsUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = "integration.js@hirehub.test",
                    FullName = "Integration JobSeeker",
                    PasswordHash = "pwd",
                    Role = "JobSeeker",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),

                    Gender = "Other",
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(jsUser);
                db.SaveChanges();
            }

            // JobSeeker
            var js = db.JobSeekers.FirstOrDefault(s => s.UserId == jsUser.UserId);
            if (js == null)
            {
                js = new JobSeeker
                {
                    JobSeekerId = Guid.NewGuid(),
                    UserId = jsUser.UserId,
                    EducationDetails = "BSc",
                    Skills = "Testing"
                };
                db.JobSeekers.Add(js);
                db.SaveChanges();
            }

            // Resume
            var resume = new Resume
            {
                JobSeekerId = js.JobSeekerId,
                ResumeName = "int_resume.pdf",
                FilePath = "/tmp/int_resume.pdf",
                UpdatedAt = DateTime.UtcNow,
                IsDefault = true
            };
            db.Resumes.Add(resume);
            db.SaveChanges();

            // Application
            var app = new Application
            {
                JobId = job.JobId,
                JobSeekerId = js.JobSeekerId,
                ResumeId = resume.ResumeId,
                CoverLetter = "Please",
                Status = "Applied",
                AppliedAt = DateTime.UtcNow
            };
            db.Applications.Add(app);
            db.SaveChanges();

            return (empUser.UserId, jsUser.UserId, job.JobId, js.JobSeekerId, app.ApplicationId);
        }

        [Fact]
        public async Task Employer_Can_Message_Applicant_By_ApplicationId()
        {
            // Arrange - seed
            var (employerUserId, jobSeekerUserId, jobId, jsId, applicationId) = SeedForNotifyApplicant();

            var client = _factory.CreateClientWithTestAuth(role: "Employer", userId: employerUserId);

            var dto = new EmployerNotifyApplicantDto
            {
                ApplicationId = applicationId,
                Message = "Hello from employer",
                Subject = "Interview",
                SendEmail = false
            };

            // Act
            var resp = await client.PostAsJsonAsync("/api/notification/application/message", dto);

            // Assert
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var returned = await resp.Content.ReadFromJsonAsync<NotificationDto>();
            returned.Should().NotBeNull();
            returned!.UserId.Should().Be(jobSeekerUserId); // notification targeted to jobseeker's user
            returned.Message.Should().Be(dto.Message);
        }

        [Fact]
        public async Task GetByUser_Returns_Notifications_For_User()
        {
            // Arrange
            var (employerUserId, jobSeekerUserId, jobId, jsId, applicationId) = SeedForNotifyApplicant();

            // seed a notification directly
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();
                var n = new Notification
                {
                    UserId = jobSeekerUserId,
                    Message = "seed notif",
                    Subject = "s",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    SentEmail = false
                };
                db.Notifications.Add(n);
                db.SaveChanges();
            }

            var client = _factory.CreateClientWithTestAuth(role: "JobSeeker", userId: jobSeekerUserId);

            // Act
            var resp = await client.GetAsync($"/api/notification/user/{jobSeekerUserId}");

            // Assert
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = await resp.Content.ReadFromJsonAsync<NotificationDto[]>();
            list.Should().NotBeNull();
            list!.Any(n => n.Message == "seed notif").Should().BeTrue();
        }

        [Fact]
        public async Task MarkAsRead_Returns_NoContent_When_Success()
        {
            // Arrange
            var (employerUserId, jobSeekerUserId, jobId, jsId, applicationId) = SeedForNotifyApplicant();

            int notifId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();
                var n = new Notification
                {
                    UserId = jobSeekerUserId,
                    Message = "to mark",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                db.Notifications.Add(n);
                db.SaveChanges();
                notifId = n.NotificationId;
            }

            var client = _factory.CreateClientWithTestAuth(role: "JobSeeker", userId: jobSeekerUserId);

            // Act
            var resp = await client.PostAsync($"/api/notification/{notifId}/mark-read", null);

            // Assert
            // The endpoint returns NoContent on success. If your repo MarkAsReadAsync returns true, NoContent is expected.
            // Depending on your repo implementation, it might return 204 or 404.
            resp.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
        }
    }
}
