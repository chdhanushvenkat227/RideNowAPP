using System.ComponentModel.DataAnnotations;

namespace RideNowAPI.DTOs
{
    public class UpdateDriverProfileDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Phone { get; set; } = string.Empty;
        
        [Required]
        public string Gender { get; set; } = string.Empty;
        
        public string? BloodGroup { get; set; }
        public string? Address { get; set; }
    }

    public class DriverProfileDto
    {
        public Guid DriverId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? LicenseNumber { get; set; }
        public DateTime LicenseExpiryDate { get; set; }
        public string? BloodGroup { get; set; }
        public string? Address { get; set; }
        public string? Location { get; set; }
        public string? VehicleType { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}