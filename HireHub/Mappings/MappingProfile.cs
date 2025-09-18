using AutoMapper;
using HireHub.API.DTOs;
using HireHub.API.Models;

namespace HireHub.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ----------------- USER -----------------
            CreateMap<User, UserDto>();

            CreateMap<CreateUserDto, User>()
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.PasswordHash, opt => opt.Ignore()) 
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Employer, opt => opt.Ignore())
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Notifications, opt => opt.Ignore());

            CreateMap<UpdateUserDto, User>()
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.Email, opt => opt.Ignore()) 
                .ForMember(d => d.PasswordHash, opt => opt.Ignore()) 
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Employer, opt => opt.Ignore())
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Notifications, opt => opt.Ignore());

            // ----------------- EMPLOYER -----------------

            // Employer → EmployerDto (full detail with user info)
            CreateMap<Employer, EmployerDto>()
                 .ForMember(d => d.UserFullName, opt => opt.MapFrom(s => s.User.FullName))
                .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.User.Email))
                .ForMember(d => d.Position, opt => opt.MapFrom(s => s.Position));

            // Employer → EmployerDisplayDto (for GET/list endpoints)
            CreateMap<Employer, EmployerDisplayDto>()
                .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.CompanyName))
                .ForMember(d => d.ContactInfo, opt => opt.MapFrom(s => s.ContactInfo))
                .ForMember(d => d.Position, opt => opt.MapFrom(s => s.Position));

            // CreateEmployerDto → Employer
            CreateMap<CreateEmployerDto, Employer>()
                .ForMember(d => d.EmployerId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.Jobs, opt => opt.Ignore());

            // UpdateEmployerDto → Employer
            CreateMap<UpdateEmployerDto, Employer>()
                .ForMember(d => d.EmployerId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.Jobs, opt => opt.Ignore());



            // JobSeeker mappings

            // Entity -> full DTO (e.g., when returning single record with ids)
            CreateMap<JobSeeker, JobSeekerDto>()
               .ForMember(d => d.UserFullName, opt => opt.MapFrom(s => s.User.FullName))
                .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.User.Email))
                .ForMember(d => d.EducationDetails, opt => opt.MapFrom(s => s.EducationDetails))
                .ForMember(d => d.Skills, opt => opt.MapFrom(s => s.Skills))
                .ForMember(d => d.College, opt => opt.MapFrom(s => s.College))
                .ForMember(d => d.WorkStatus, opt => opt.MapFrom(s => s.WorkStatus))
                .ForMember(d => d.Experience, opt => opt.MapFrom(s => s.Experience));

            // Entity -> display DTO (for GET/list endpoints)
            CreateMap<JobSeeker, JobSeekerDisplayDto>()
                .ForMember(d => d.UserFullName, opt => opt.MapFrom(s => s.User.FullName))
                .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.User.Email))
                .ForMember(d => d.EducationDetails, opt => opt.MapFrom(s => s.EducationDetails))
                .ForMember(d => d.Skills, opt => opt.MapFrom(s => s.Skills))
                .ForMember(d => d.College, opt => opt.MapFrom(s => s.College))
                .ForMember(d => d.WorkStatus, opt => opt.MapFrom(s => s.WorkStatus))
                .ForMember(d => d.Experience, opt => opt.MapFrom(s => s.Experience));

            // Create DTO -> Entity (for POST)
            CreateMap<CreateJobSeekerDto, JobSeeker>()
                .ForMember(d => d.JobSeekerId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())        
                .ForMember(d => d.Resumes, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore())
                // map DTO names to entity names if they differ
                .ForMember(d => d.EducationDetails, opt => opt.MapFrom(s => s.EducationDetails))
                .ForMember(d => d.Experience, opt => opt.MapFrom(s => s.Experience));

            // Update DTO -> Entity (for PUT/PATCH)
            CreateMap<UpdateJobSeekerDto, JobSeeker>()
                .ForMember(d => d.JobSeekerId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.Resumes, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore())
                .ForMember(d => d.EducationDetails, opt => opt.MapFrom(s => s.EducationDetails))
                .ForMember(d => d.Experience, opt => opt.MapFrom(s => s.Experience));


            // ----------------- JOB -----------------
            CreateMap<Job, JobDto>()
                .ForMember(d => d.EmployerName, opt => opt.MapFrom(s => s.Employer.CompanyName));

            // CreateJobDto -> Job
            CreateMap<CreateJobDto, Job>()
                .ForMember(d => d.JobId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.Employer, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore());

            // UpdateJobDto -> Job
            CreateMap<UpdateJobDto, Job>()
                .ForMember(d => d.JobId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.EmployerId, opt => opt.Ignore())
                .ForMember(d => d.Employer, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore());


            // ----------------- RESUME -----------------
          
            CreateMap<Resume, ResumeDto>()
               .ForMember(d => d.JobSeekerName,
                    opt => opt.MapFrom(s => s.JobSeeker != null && s.JobSeeker.User != null
                                   ? s.JobSeeker.User.FullName
                                   : string.Empty));


            // CreateResumeDto -> Resume
            CreateMap<CreateResumeDto, Resume>()
                .ForMember(d => d.ResumeId, opt => opt.Ignore())
                .ForMember(d => d.UpdatedAt, opt => opt.Ignore()) // set UpdatedAt in service on create
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore());

            // UpdateResumeDto -> Resume
            CreateMap<UpdateResumeDto, Resume>()
                .ForMember(d => d.ResumeId, opt => opt.Ignore())
                .ForMember(d => d.UpdatedAt, opt => opt.Ignore()) // update UpdatedAt in service on update
                .ForMember(d => d.JobSeekerId, opt => opt.Ignore())
                .ForMember(d => d.JobSeeker, opt => opt.Ignore())
                .ForMember(d => d.Applications, opt => opt.Ignore());


            // ----------------- APPLICATION -----------------
            // Application -> ApplicationDto
            CreateMap<Application, ApplicationDto>()
                .ForMember(d => d.JobTitle, opt => opt.MapFrom(s => s.Job != null ? s.Job.Title : string.Empty))
                .ForMember(d => d.JobSeekerName,
                    opt => opt.MapFrom(s => s.JobSeeker != null && s.JobSeeker.User != null
                                   ? s.JobSeeker.User.FullName
                                   : string.Empty));

            // CreateApplicationDto -> Application
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
            // ----------------- APPLICATION -----------------
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
                .ForMember(d => d.IsRead, opt => opt.Ignore()) // always false on create
                .ForMember(d => d.SentEmail, opt => opt.Ignore()) // set in service
                .ForMember(d => d.User, opt => opt.Ignore());

            // EmployerNotifyApplicantDto → Notification
            CreateMap<EmployerNotifyApplicantDto, Notification>()
                .ForMember(d => d.NotificationId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore()) // resolved via Application
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.IsRead, opt => opt.Ignore())
                .ForMember(d => d.SentEmail, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore());

            // JobSeekerNotifyEmployerDto → Notification
            CreateMap<JobSeekerNotifyEmployerDto, Notification>()
                .ForMember(d => d.NotificationId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore()) // resolved via Job → Employer
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.IsRead, opt => opt.Ignore())
                .ForMember(d => d.SentEmail, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore());

            // UpdateNotificationDto → Notification
            CreateMap<UpdateNotificationDto, Notification>()
                .ForMember(d => d.NotificationId, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore());

        }
    }
}
