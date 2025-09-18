using System;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }   

        [Required]
        public Guid UserId { get; set; }       

        [Required, MaxLength(300)]
        public string Message { get; set; } = string.Empty;  

        public bool IsRead { get; set; } = false; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 

      
        [MaxLength(100)]
        public string? Subject { get; set; }      

        public bool SentEmail { get; set; } = false; 

        // Navigation
        public User User { get; set; } = null!;
    }
}
