using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideNowAPI.Models
{
    public class DriverEarnings
    {
        [Key]
        public Guid EarningId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DriverId { get; set; }

        [Required]
        public Guid RideId { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Fare { get; set; }

        [StringLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [ForeignKey("DriverId")]
        public virtual Driver Driver { get; set; } = null!;

        [ForeignKey("RideId")]
        public virtual Ride Ride { get; set; } = null!;
    }
}
