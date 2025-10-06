using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HireHub.API.Models
{
    public class User
    {
        public Guid UserId { get; set; }

        [Required, MaxLength(50)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty; 

        [Required]
        [RegularExpression("Employer|JobSeeker|Admin")]
        public string Role { get; set; } = string.Empty;

        [Required] [Column(TypeName="date")]
        public DateOnly DateOfBirth { get; set; }

        [Required, MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public Employer? Employer { get; set; }
        public JobSeeker? JobSeeker { get; set; }
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
