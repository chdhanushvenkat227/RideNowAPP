using System.ComponentModel.DataAnnotations;

namespace RideNowAPP.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserType { get; set; } = string.Empty; // "User" or "Driver"
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}