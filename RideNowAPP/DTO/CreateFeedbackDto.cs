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
        
        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}