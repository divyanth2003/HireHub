using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.Models
{
    public class Employer
    {
        public Guid EmployerId { get; set; }

        public Guid UserId { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Position { get; set; }

        [MaxLength(200)]
        public string? ContactInfo { get; set; }

        

        // Navigation
        public User User { get; set; } = null!;
        public ICollection<Job> Jobs { get; set; } = new List<Job>();
    }
}
