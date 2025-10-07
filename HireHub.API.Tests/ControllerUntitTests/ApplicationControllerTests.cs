using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HireHub.API.Controllers;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;

namespace HireHub.API.Tests.Controllers
{
    public class ApplicationControllerTests
    {
        private readonly Mock<IApplicationRepository> _appRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ApplicationService>> _loggerMock;
        private readonly Mock<INotificationRepository> _notificationRepoMock;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<ILogger<NotificationService>> _notificationLoggerMock;

        private readonly ApplicationService _applicationService;
        private readonly NotificationService _notificationService;
        private readonly ApplicationController _sut;

        public ApplicationControllerTests()
        {
            _appRepoMock = new Mock<IApplicationRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ApplicationService>>();
            _notificationRepoMock = new Mock<INotificationRepository>();
            _emailMock = new Mock<IEmailService>();
            _notificationLoggerMock = new Mock<ILogger<NotificationService>>();

            
            _notificationService = new NotificationService(
                _notificationRepoMock.Object,
                _appRepoMock.Object,
                _emailMock.Object,
                _mapperMock.Object,
                _notificationLoggerMock.Object
            );

           
            _applicationService = new ApplicationService(
                _appRepoMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _notificationService
            );

          
            _sut = new ApplicationController(_applicationService);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithMappedDtos()
        {
            var apps = new List<Application> { new Application { ApplicationId = 1 } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 1 } };

            _appRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(apps);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(apps)).Returns(dtos);

            var result = await _sut.GetAll();

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetById_Existing_ReturnsOkObjectResult()
        {
            var entity = new Application { ApplicationId = 5 };
            var dto = new ApplicationDto { ApplicationId = 5 };

            _appRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<ApplicationDto>(entity)).Returns(dto);

            var result = await _sut.GetById(5);

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public async Task GetByJob_ReturnsOkWithMapped()
        {
            var jobId = 10;
            var list = new List<Application> { new Application { ApplicationId = 2, JobId = jobId } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 2 } };

            _appRepoMock.Setup(r => r.GetByJobAsync(jobId)).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(list)).Returns(dtos);

            var result = await _sut.GetByJob(jobId);

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetByJobSeeker_ReturnsOkWithMapped()
        {
            var jsId = Guid.NewGuid();
            var list = new List<Application> { new Application { ApplicationId = 3, JobSeekerId = jsId } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 3 } };

            _appRepoMock.Setup(r => r.GetByJobSeekerAsync(jsId)).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(list)).Returns(dtos);

            var result = await _sut.GetByJobSeeker(jsId);

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetShortlisted_ReturnsOk()
        {
            var jobId = 11;
            var list = new List<Application> { new Application { ApplicationId = 7 } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 7 } };

            _appRepoMock.Setup(r => r.GetShortlistedByJobAsync(jobId)).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(list)).Returns(dtos);

            var result = await _sut.GetShortlisted(jobId);

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task GetWithInterview_ReturnsOk()
        {
            var jobId = 12;
            var list = new List<Application> { new Application { ApplicationId = 9 } };
            var dtos = new List<ApplicationDto> { new ApplicationDto { ApplicationId = 9 } };

            _appRepoMock.Setup(r => r.GetWithInterviewAsync(jobId)).ReturnsAsync(list);
            _mapperMock.Setup(m => m.Map<IEnumerable<ApplicationDto>>(list)).Returns(dtos);

            var result = await _sut.GetWithInterview(jobId);

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtAction()
        {
            var dtoIn = new CreateApplicationDto { JobId = 1, JobSeekerId = Guid.NewGuid() };
            var entity = new Application { ApplicationId = 21, JobId = 1 };
            var returnedDto = new ApplicationDto { ApplicationId = 21, JobId = 1 };

            _mapperMock.Setup(m => m.Map<Application>(dtoIn)).Returns(entity);
            _appRepoMock.Setup(r => r.AddAsync(It.IsAny<Application>())).ReturnsAsync(entity);
            _appRepoMock.Setup(r => r.GetByIdAsync(entity.ApplicationId)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<ApplicationDto>(entity)).Returns(returnedDto);

            var result = await _sut.Create(dtoIn);

            var created = result as CreatedAtActionResult;
            created.Should().NotBeNull();
            created!.Value.Should().BeEquivalentTo(returnedDto);
        }

        [Fact]
        public async Task Update_ReturnsOk()
        {
            
            var id = 33;

            var dto = new UpdateApplicationDto
            {
                Status = "Shortlisted",
                CoverLetter = "Congrats!"
               
            };

         
            var jobSeekerUserId = Guid.NewGuid();
            var employerUserId = Guid.NewGuid();

            var existingEntity = new Application
            {
                ApplicationId = id,
                Status = "Applied",
                JobSeeker = new JobSeeker
                {
                    User = new User
                    {
                        UserId = jobSeekerUserId,
                        FullName = "Alex JobSeeker",
                        Email = "jobseeker@example.com"
                    }
                },
                Job = new Job
                {
                    JobId = 7,
                    Title = "Software Engineer",
                    Employer = new Employer
                    {
                        CompanyName = "Acme Co",
                        User = new User
                        {
                            UserId = employerUserId,
                            FullName = "Sam Employer",
                            Email = "employer@example.com"
                        }
                    }
                }
            };

      
            var updatedDto = new ApplicationDto
            {
                ApplicationId = id,
                JobId = existingEntity.Job.JobId,
                JobSeekerId = jobSeekerUserId,
                JobTitle = existingEntity.Job.Title,
                JobSeekerName = existingEntity.JobSeeker.User.FullName,
                Status = "Shortlisted",
                AppliedAt = DateTime.UtcNow
            };

            
            _appRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingEntity);
            _mapperMock.Setup(m => m.Map(dto, existingEntity))
                       .Callback<UpdateApplicationDto, Application>((d, e) => e.Status = d.Status);
            _appRepoMock.Setup(r => r.UpdateAsync(existingEntity)).ReturnsAsync(existingEntity);
            _mapperMock.Setup(m => m.Map<ApplicationDto>(existingEntity)).Returns(updatedDto);
            
            var result = await _sut.Update(id, dto);

       
            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(updatedDto);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenExists()
        {
            var id = 10;
            _appRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

           
            var result = await _sut.Delete(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task MarkReviewed_ReturnsOk()
        {
            var id = 99;
            var app = new Application { ApplicationId = id, Notes = "note", ReviewedAt = DateTime.UtcNow };
            var dto = new ApplicationDto { ApplicationId = id, Notes = "note" };

            _appRepoMock.Setup(r => r.MarkReviewedAsync(id, It.IsAny<string?>())).ReturnsAsync(app);
            _mapperMock.Setup(m => m.Map<ApplicationDto>(app)).Returns(dto);

            var result = await _sut.MarkReviewed(id, "ok");

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            ok!.Value.Should().BeEquivalentTo(dto);
        }
    }
}
