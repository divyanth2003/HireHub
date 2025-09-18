using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.Models
{
    public class JobSeeker
    {
        public Guid JobSeekerId { get; set; }
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



        // Navigation
        public User User { get; set; } = null!;
        public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
