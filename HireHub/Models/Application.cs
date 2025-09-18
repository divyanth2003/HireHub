using HireHub.API.Models;
using System.ComponentModel.DataAnnotations;

public class Application
{
    public int ApplicationId { get; set; }
    public int JobId { get; set; }
    public Guid JobSeekerId { get; set; }
    public int ResumeId { get; set; }

    public string? CoverLetter { get; set; }

    [Required]
    public string Status { get; set; } = "Applied"; 

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Notes { get; set; }  
    public DateTime? ReviewedAt { get; set; } 

    public bool IsShortlisted { get; set; } = false;

    public DateTime? InterviewDate { get; set; }

    [MaxLength(1000)]
    public string? EmployerFeedback { get; set; }

    // Navigation
    public Job Job { get; set; } = null!;
    public JobSeeker JobSeeker { get; set; } = null!;
    public Resume Resume { get; set; } = null!;
}
