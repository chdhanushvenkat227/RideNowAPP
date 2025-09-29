namespace RideNowAPI.DTO
{
    public class CreateFeedbackDto
    {
        public Guid RideId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}