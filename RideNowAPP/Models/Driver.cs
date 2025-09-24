using System.ComponentModel.DataAnnotations;

namespace RideNowAPI.Models
{
    public enum DriverStatus
    {
        Available,
        Unavailable,
        Riding
    }

    public class Driver
    {
        [Key]
        public Guid DriverId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;

        [StringLength(15)]
        public string? LicenseNumber { get; set; }

        public DateTime? LicenseExpiryDate { get; set; }

        [StringLength(5)]
        public string? BloodGroup { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string Location { get; set; } = string.Empty;

        [StringLength(20)]
        public string VehicleType { get; set; } = string.Empty;

        public DriverStatus Status { get; set; } = DriverStatus.Unavailable;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Ride> Rides { get; set; } = new List<Ride>();
        public virtual ICollection<Rating> DriverRatings { get; set; } = new List<Rating>();
        public virtual ICollection<DriverEarnings> Earnings { get; set; } = new List<DriverEarnings>();
    }
}
