// Models/Resume.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // use System.Text.Json explicitly

namespace HireHub.API.Models
{
    public class Resume
    {
        public int ResumeId { get; set; }
        public Guid JobSeekerId { get; set; }

        [Required, MaxLength(150)]
        public string ResumeName { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? FileType { get; set; }

        public bool IsDefault { get; set; } = false;

        [MaxLength(800)]
        public string? ParsedSkills { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties that may create cycles — hide from JSON.
        [JsonIgnore]
        public JobSeeker? JobSeeker { get; set; }

        [JsonIgnore]
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
