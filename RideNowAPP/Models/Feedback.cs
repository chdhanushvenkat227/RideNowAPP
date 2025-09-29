using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideNowAPI.Models
{
    public class Feedback
    {
        [Key]
        public Guid FeedbackId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RideId { get; set; }

        [Required]
        public Guid DriverId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("RideId")]
        public virtual Ride Ride { get; set; } = null!;

        [ForeignKey("DriverId")]
        public virtual Driver Driver { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}