using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideNowAPI.Models
{
    public class Rating
    {
        [Key]
        public Guid RatingId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RideId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid DriverId { get; set; }

        [Range(1, 5)]
        public int UserRating { get; set; }

        [Range(1, 5)]
        public int DriverRating { get; set; }

        [StringLength(500)]
        public string UserComment { get; set; } = string.Empty;

        [StringLength(500)]
        public string DriverComment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("RideId")]
        public virtual Ride Ride { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("DriverId")]
        public virtual Driver Driver { get; set; } = null!;
    }
}
