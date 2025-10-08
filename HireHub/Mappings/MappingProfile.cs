using AutoMapper;
using HireHub.API.DTOs;
using HireHub.API.Models;
using System;

namespace HireHub.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();

          
            CreateMap<CreateUserDto, User>()
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.PasswordHash, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Employer, opt => opt.Ignore())
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Notifications, opt => opt.Ignore())
             
                .ForMember(d => d.IsActive, opt => opt.Ignore())
                .ForMember(d => d.DeactivatedAt, opt => opt.Ignore());

         
            CreateMap<UpdateUserDto, User>()
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.Email, opt => opt.Ignore())
                .ForMember(d => d.PasswordHash, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Employer, opt => opt.Ignore())
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Notifications, opt => opt.Ignore())
                .ForMember(d => d.IsActive, opt => opt.Ignore())
                .ForMember(d => d.DeactivatedAt, opt => opt.Ignore());

            // Employer mappings...
            CreateMap<Employer, EmployerDto>()
                .ForMember(d => d.UserFullName, opt => opt.MapFrom(s => s.User.FullName))
                .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.User.Email))
                .ForMember(d => d.Position, opt => opt.MapFrom(s => s.Position));

            CreateMap<Employer, EmployerDisplayDto>()
                .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.CompanyName))
                .ForMember(d => d.ContactInfo, opt => opt.MapFrom(s => s.ContactInfo))
                .ForMember(d => d.Position, opt => opt.MapFrom(s => s.Position));

            CreateMap<CreateEmployerDto, Employer>()
                .ForMember(d => d.EmployerId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.Jobs, opt => opt.Ignore());

            CreateMap<UpdateEmployerDto, Employer>()
                .ForMember(d => d.EmployerId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.Jobs, opt => opt.Ignore());

            // JobSeeker mappings...
            CreateMap<JobSeeker, JobSeekerDto>()
               .ForMember(d => d.UserFullName, opt => opt.MapFrom(s => s.User.FullName))
               .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.User.Email))
               .ForMember(d => d.EducationDetails, opt => opt.MapFrom(s => s.EducationDetails))
               .ForMember(d => d.Skills, opt => opt.MapFrom(s => s.Skills))
               .ForMember(d => d.College, opt => opt.MapFrom(s => s.College))
               .ForMember(d => d.WorkStatus, opt => opt.MapFrom(s => s.WorkStatus))
               .ForMember(d => d.Experience, opt => opt.MapFrom(s => s.Experience));

            CreateMap<JobSeeker, JobSeekerDisplayDto>()
                .ForMember(d => d.UserFullName, opt => opt.MapFrom(s => s.User.FullName))
                .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.User.Email))
                .ForMember(d => d.EducationDetails, opt => opt.MapFrom(s => s.EducationDetails))
                .ForMember(d => d.Skills, opt => opt.MapFrom(s => s.Skills))
                .ForMember(d => d.College, opt => opt.MapFrom(s => s.College))
                .ForMember(d => d.WorkStatus, opt => opt.MapFrom(s => s.WorkStatus))
                .ForMember(d => d.Experience, opt => opt.MapFrom(s => s.Experience));

            CreateMap<CreateJobSeekerDto, JobSeeker>()
                .ForMember(d => d.JobSeekerId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.Resumes, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore())
                .ForMember(d => d.EducationDetails, opt => opt.MapFrom(s => s.EducationDetails))
                .ForMember(d => d.Experience, opt => opt.MapFrom(s => s.Experience));

            CreateMap<UpdateJobSeekerDto, JobSeeker>()
                .ForMember(d => d.JobSeekerId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.Resumes, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore())
                .ForMember(d => d.EducationDetails, opt => opt.MapFrom(s => s.EducationDetails))
                .ForMember(d => d.Experience, opt => opt.MapFrom(s => s.Experience));

            // Job mapping (kept as you had it)
            CreateMap<Job, JobDto>()
                .ForMember(d => d.EmployerName, opt => opt.MapFrom(s => s.Employer != null
                    ? (s.Employer.CompanyName ?? s.Employer.User.FullName ?? string.Empty)
                    : string.Empty));

            CreateMap<CreateJobDto, Job>()
                .ForMember(d => d.JobId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Employer, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore());

            CreateMap<UpdateJobDto, Job>()
                .ForMember(d => d.JobId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.EmployerId, opt => opt.Ignore())
                .ForMember(d => d.Employer, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore());

         
            CreateMap<Resume, ResumeDto>()
                .ForMember(d => d.JobSeekerName,
                    opt => opt.MapFrom(s => s.JobSeeker != null && s.JobSeeker.User != null
                        ? s.JobSeeker.User.FullName
                        : string.Empty));

            CreateMap<CreateResumeDto, Resume>()
                .ForMember(d => d.ResumeId, opt => opt.Ignore())
                .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore());

            CreateMap<UpdateResumeDto, Resume>()
                .ForMember(d => d.ResumeId, opt => opt.Ignore())
                .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
                .ForMember(d => d.JobSeekerId, opt => opt.Ignore())
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore());

         
            CreateMap<Application, ApplicationDto>()
                .ForMember(d => d.JobTitle, opt => opt.MapFrom(s => s.Job != null ? s.Job.Title : string.Empty))
                .ForMember(d => d.JobSeekerName,
                    opt => opt.MapFrom(s => s.JobSeeker != null && s.JobSeeker.User != null
                                   ? s.JobSeeker.User.FullName
                                   : string.Empty));

            CreateMap<CreateApplicationDto, Application>()
                .ForMember(d => d.ApplicationId, opt => opt.Ignore())
                .ForMember(d => d.AppliedAt, opt => opt.Ignore())
                .ForMember(d => d.Job, opt => opt.Ignore())
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Resume, opt => opt.Ignore())
                .ForMember(d => d.ReviewedAt, opt => opt.Ignore())
                .ForMember(d => d.Notes, opt => opt.Ignore())
                .ForMember(d => d.IsShortlisted, opt => opt.Ignore())
                .ForMember(d => d.InterviewDate, opt => opt.Ignore())
                .ForMember(d => d.EmployerFeedback, opt => opt.Ignore());

            CreateMap<UpdateApplicationDto, Application>()
                .ForMember(d => d.ApplicationId, opt => opt.Ignore())
                .ForMember(d => d.JobId, opt => opt.Ignore())
                .ForMember(d => d.JobSeekerId, opt => opt.Ignore())
                .ForMember(d => d.ResumeId, opt => opt.Ignore())
                .ForMember(d => d.AppliedAt, opt => opt.Ignore())
                .ForMember(d => d.Job, opt => opt.Ignore())
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Resume, opt => opt.Ignore());

            CreateMap<Notification, NotificationDto>()
                .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.User.Email));

            CreateMap<CreateNotificationDto, Notification>()
                .ForMember(d => d.NotificationId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.IsRead, opt => opt.Ignore())
                .ForMember(d => d.SentEmail, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore());

            CreateMap<EmployerNotifyApplicantDto, Notification>()
                .ForMember(d => d.NotificationId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.IsRead, opt => opt.Ignore())
                .ForMember(d => d.SentEmail, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore());

            CreateMap<JobSeekerNotifyEmployerDto, Notification>()
                .ForMember(d => d.NotificationId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.IsRead, opt => opt.Ignore())
                .ForMember(d => d.SentEmail, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore());

            CreateMap<UpdateNotificationDto, Notification>()
                .ForMember(d => d.NotificationId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore());
        }
    }
}
