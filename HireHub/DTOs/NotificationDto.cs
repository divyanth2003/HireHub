using System;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.DTOs
{
    // What you return to clients
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public Guid UserId { get; set; }              
        public string Message { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public bool IsRead { get; set; }
        public bool SentEmail { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public string UserEmail { get; set; } = string.Empty;
    }

    }

    public class CreateNotificationDto
    {
        [Required]
        public Guid UserId { get; set; }              

        [Required, MaxLength(300)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Subject { get; set; }

        public bool SendEmail { get; set; } = false;  
    }


    public class EmployerNotifyApplicantDto
    {
        [Required]
        public int ApplicationId { get; set; }        

        [Required, MaxLength(300)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Subject { get; set; }

        public bool SendEmail { get; set; } = true;
    }

   
    public class JobSeekerNotifyEmployerDto
    {
        [Required]
        public int JobId { get; set; }                

        [Required, MaxLength(300)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Subject { get; set; }

        public bool SendEmail { get; set; } = true;
    }

    // For marking notification as read, or optional update
    public class UpdateNotificationDto
    {
        public bool IsRead { get; set; }

        [MaxLength(300)]
        public string? Message { get; set; }          // optional edit
    }

