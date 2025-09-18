using System;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.DTOs
{
    public class JobSeekerDto
    {
        public Guid JobSeekerId { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? EducationDetails { get; set; }
        public string? Skills { get; set; }
        public string? College { get; set; }
        public string? WorkStatus { get; set; }
        public string? Experience { get; set; }
    }

    public class JobSeekerDisplayDto
    {
        //public Guid JobSeekerId { get; set; }
       //public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? EducationDetails { get; set; }
        public string? Skills { get; set; }
        public string? College { get; set; }
        public string? WorkStatus { get; set; }
        public string? Experience { get; set; }
    }

    public class CreateJobSeekerDto
    {
        [Required]
        public Guid UserId { get; set; }

        [MaxLength(300)]
        public string? EducationDetails { get; set; }

        [MaxLength(500)]
        public string? Skills { get; set; }

        [MaxLength(100)]
        public string? College { get; set; }

        [MaxLength(50)]
        public string? WorkStatus { get; set; }

        [MaxLength(100)]
        public string? Experience { get; set; }
    }

    public class UpdateJobSeekerDto
    {
        [MaxLength(300)]
        public string? EducationDetails { get; set; }

        [MaxLength(500)]
        public string? Skills { get; set; }

        [MaxLength(100)]
        public string? College { get; set; }

        [MaxLength(50)]
        public string? WorkStatus { get; set; }

        [MaxLength(100)]
        public string? Experience { get; set; }
    }
}
