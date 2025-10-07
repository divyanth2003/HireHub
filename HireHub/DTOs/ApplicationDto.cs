using System;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.DTOs
{
    public class ApplicationDto
    {
        public int ApplicationId { get; set; }
        public int JobId { get; set; }
        public Guid JobSeekerId { get; set; }

        public string JobTitle { get; set; } = string.Empty;
        public string JobSeekerName { get; set; } = string.Empty;
        public int? ResumeId { get; set; }
        public string? CoverLetter { get; set; }
        public string Status { get; set; } = "Applied";
        public DateTime AppliedAt { get; set; }

      
        public DateTime? ReviewedAt { get; set; }     
        public string? Notes { get; set; }             
        public bool IsShortlisted { get; set; } = false;
        public DateTime? InterviewDate { get; set; }   
        public string? EmployerFeedback { get; set; }  

    }

    public class CreateApplicationDto
    {
        [Required]
        public int JobId { get; set; }

        [Required]
        public Guid JobSeekerId { get; set; }

        public int? ResumeId { get; set; }

        public string? CoverLetter { get; set; }

    }

    public class UpdateApplicationDto
    {
        [Required]
        public string Status { get; set; } = "Applied";

        public string? CoverLetter { get; set; }

       
        public bool? IsShortlisted { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? EmployerFeedback { get; set; }
    }
}
