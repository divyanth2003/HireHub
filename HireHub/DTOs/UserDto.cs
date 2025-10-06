using System;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.DTOs
{
    // ------------------- OUTPUT DTO -------------------
    public class UserDto
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

   
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }

    // ------------------- CREATE DTO -------------------
    public class CreateUserDto
    {
        [Required, MaxLength(50)]
        public string FullName { get; set; } = string.Empty;


        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6),MaxLength(20)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("Employer|JobSeeker|Admin", ErrorMessage = "Role must be Employer, JobSeeker or Admin")]
        public string Role { get; set; } = "JobSeeker";

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required, MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;
    }

    // ------------------- UPDATE DTO -------------------
    public class UpdateUserDto
    {
        [Required, MaxLength(50)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [RegularExpression("Employer|JobSeeker|Admin", ErrorMessage = "Role must be Employer, JobSeeker or Admin")]
        public string Role { get; set; } = "JobSeeker";

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required, MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;
    }
}
