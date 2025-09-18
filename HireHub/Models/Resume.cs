using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.Models
{
    public class Resume
    {
        public int ResumeId { get; set; }

        public Guid JobSeekerId { get; set; }

        [Required, MaxLength(150)]
        public string ResumeName { get; set; } = string.Empty;   // e.g., "Divyan_Resume.pdf"

        [Required, MaxLength(300)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? FileType { get; set; }  

        public bool IsDefault { get; set; } = false;

        [MaxLength(800)]
        public string? ParsedSkills { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public JobSeeker JobSeeker { get; set; } = null!;
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
