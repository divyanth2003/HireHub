using System;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.DTOs
{
    public class ResumeDto
    {
        public int ResumeId { get; set; }
        public Guid JobSeekerId { get; set; }

        [Required, MaxLength(150)]
        public string ResumeName { get; set; } = string.Empty;   // new

        [Required, MaxLength(300)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(800)]
        public string? ParsedSkills { get; set; }

        public DateTime UpdatedAt { get; set; }

        [MaxLength(10)]
        public string? FileType { get; set; }    // new

        public bool IsDefault { get; set; }      
        public string JobSeekerName { get; set; } = string.Empty;
    }

    public class CreateResumeDto
    {
        [Required]
        public Guid JobSeekerId { get; set; }

        [Required, MaxLength(150)]
        public string ResumeName { get; set; } = string.Empty;   // new

        [Required, MaxLength(300)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(800)]
        public string? ParsedSkills { get; set; }

        [MaxLength(10)]
        public string? FileType { get; set; }    // new

        // Optional: allow client to mark uploaded resume as default
        public bool IsDefault { get; set; } = false;
    }

    public class UpdateResumeDto
    {
        [Required, MaxLength(150)]
        public string ResumeName { get; set; } = string.Empty;   // new

        [Required, MaxLength(300)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(800)]
        public string? ParsedSkills { get; set; }

        [MaxLength(10)]
        public string? FileType { get; set; }    // new

        public bool IsDefault { get; set; } = false;
    }
}
