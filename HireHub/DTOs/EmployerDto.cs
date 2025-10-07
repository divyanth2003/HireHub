using System;
using System.ComponentModel.DataAnnotations;

namespace HireHub.API.DTOs
{
    public class EmployerDto
    {
        public Guid EmployerId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }

        public string? Position { get; set; }  

        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }

    public class EmployerDisplayDto 
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }

        public string? Position { get; set; }
    }

    public class CreateEmployerDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ContactInfo { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; }   
    }

    public class UpdateEmployerDto
    {
        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ContactInfo { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; }  
    }
}
