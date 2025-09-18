using System;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.DTOs
{
    public class JobDto
    {
        public int JobId { get; set; }
        public Guid EmployerId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Location { get; set; }
        public decimal? Salary { get; set; }
        public string? SkillsRequired { get; set; }

        public string? AcademicEligibility { get; set; }   // NEW
        public string? AllowedBatches { get; set; }        // NEW
        public int? Backlogs { get; set; }                 // NEW

        public string Status { get; set; } = "Open";
        public DateTime CreatedAt { get; set; }

        // Employer Info
        public string EmployerName { get; set; } = string.Empty;
    }

    public class CreateJobDto
    {
        [Required]
        public Guid EmployerId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Location { get; set; }

        public decimal? Salary { get; set; }

        [MaxLength(500)]
        public string? SkillsRequired { get; set; }

        [MaxLength(300)]
        public string? AcademicEligibility { get; set; }   // NEW

        [MaxLength(200)]
        public string? AllowedBatches { get; set; }        // NEW

        public int? Backlogs { get; set; }                 // NEW
    }

    public class UpdateJobDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Location { get; set; }

        public decimal? Salary { get; set; }

        [MaxLength(500)]
        public string? SkillsRequired { get; set; }

        [MaxLength(300)]
        public string? AcademicEligibility { get; set; }   // NEW

        [MaxLength(200)]
        public string? AllowedBatches { get; set; }        // NEW

        public int? Backlogs { get; set; }                 // NEW

        [Required]
        public string Status { get; set; } = "Open";
    }
}
