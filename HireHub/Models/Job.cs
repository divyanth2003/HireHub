using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.Models
{
    public class Job
    {
        public int JobId { get; set; }

        public Guid EmployerId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Location { get; set; }

        public decimal? Salary { get; set; }

        [MaxLength(500)]
        public string? SkillsRequired { get; set; }

        [MaxLength(300)]
        public string? AcademicEligibility { get; set; }  

        [MaxLength(200)]
        public string? AllowedBatches { get; set; }        

        public int? Backlogs { get; set; }                 

        [MaxLength(50)]
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Employer Employer { get; set; } = null!;
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
