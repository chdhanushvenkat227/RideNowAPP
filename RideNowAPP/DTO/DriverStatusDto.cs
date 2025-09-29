using System.ComponentModel.DataAnnotations;
using RideNowAPI.Models;

namespace RideNowAPI.DTOs
{
    public class DriverStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public Guid DriverId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateDriverStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}