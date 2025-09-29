using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideNowAPI.Models
{
    public enum PaymentSelectionStatus
    {
        Selected,
        Processing,
        Completed
    }

    public class PaymentSelection
    {
        [Key]
        public Guid PaymentSelectionId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RideId { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = string.Empty; // "Cash", "UPI", "QRCode"

        [StringLength(100)]
        public string? UPIId { get; set; }

        public PaymentSelectionStatus Status { get; set; } = PaymentSelectionStatus.Selected;

        public DateTime SelectedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("RideId")]
        public virtual Ride Ride { get; set; } = null!;
    }
}