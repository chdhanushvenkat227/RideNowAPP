using System.ComponentModel.DataAnnotations;

namespace RideNowAPI.DTOs
{
    public class CreateFeedbackDto
    {
        [Required]
        public Guid RideId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;  // Removed [Required]

        [StringLength(20)]
        public string FeedbackType { get; set; } = "UserToDriver";  // NEW FIELD

        public Guid? DriverId { get; set; }  // NEW FIELD
    }
}
