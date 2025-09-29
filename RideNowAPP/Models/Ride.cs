using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideNowAPI.Models
{
    public enum RideStatus
    {
        Requested,
        Accepted,
        InProgress,
        Completed,
        Cancelled
    }

    public class Ride
    {
        [Key]
        public Guid RideId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        public Guid? DriverId { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string PickupLocation { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string DropLocation { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,8)")]
        public decimal PickupLatitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal PickupLongitude { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public decimal DropLatitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal DropLongitude { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Distance { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Fare { get; set; }

        [Required]
        [StringLength(20)]
        public string VehicleType { get; set; } = string.Empty;

        public RideStatus Status { get; set; } = RideStatus.Requested;

        [StringLength(4)]
        public string OTP { get; set; } = string.Empty;


        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [ForeignKey("UserId")]
        public  User User { get; set; } = null!;

        [ForeignKey("DriverId")]
        public  Driver? Driver { get; set; }

        public  Payment? Payment { get; set; }

    }
}
