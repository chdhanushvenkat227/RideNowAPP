using System.ComponentModel.DataAnnotations;

namespace RideNowAPI.DTOs
{
    public class DriverRegisterDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    public class DriverLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class DriverPreferencesDto
    {
        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string VehicleType { get; set; } = string.Empty;
    }

    public class DriverCompleteProfileDto
    {
        [Required]
        public string Gender { get; set; } = string.Empty;

        [StringLength(15)]
        public string? LicenseNumber { get; set; }  // ✅ Nullable now

        [Required]
        public DateTime LicenseExpiryDate { get; set; }

        public string? BloodGroup { get; set; }
        public string? Address { get; set; }
    }
}
