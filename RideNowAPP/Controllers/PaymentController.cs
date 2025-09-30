using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.Models;
using RideNowAPI.Services;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly RideNowDbContext _context;
        private readonly PaymentService _paymentService;

        public PaymentController(RideNowDbContext context, PaymentService paymentService)
        {
            _context = context;
            _paymentService = paymentService;
        }

        [HttpPost("upi/generate-qr")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateUPIQR([FromBody] UPIQRDto dto)
        {
            var ride = await _context.Rides
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.RideId == dto.RideId);

            if (ride == null)
                return BadRequest("Ride not found");

            var driverName = ride.Driver?.Name ?? "RideNow Driver";
            var qrCode = _paymentService.GenerateUPIQR(driverName, dto.Amount);
            return Ok(new { qrCode });
        }

        [HttpPost("process")]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentDto dto)
        {
            if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, out var paymentMethod))
                return BadRequest("Invalid payment method");

            var payment = await _paymentService.ProcessPayment(
                dto.RideId, dto.Amount, paymentMethod, dto.UPIId);

            return Ok(new
            {
                paymentId = payment.PaymentId,
                transactionId = payment.TransactionId,
                status = payment.Status.ToString(),
                paymentMethod = payment.PaymentMethod.ToString()
            });
        }

        [HttpGet("ride/{rideId}")]
        public async Task<IActionResult> GetRidePayment(Guid rideId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.RideId == rideId);

            return Ok(payment);
        }

        [HttpPost("select")]
        [AllowAnonymous]
        public async Task<IActionResult> SelectPaymentMethod([FromBody] PaymentSelectionDto dto)
        {
            var existingSelection = await _context.PaymentSelections
                .FirstOrDefaultAsync(ps => ps.RideId == dto.RideId);

            if (existingSelection != null)
            {
                existingSelection.PaymentMethod = dto.PaymentMethod;
                existingSelection.UPIId = dto.UPIId;
                existingSelection.Status = PaymentSelectionStatus.Selected;
                existingSelection.SelectedAt = DateTime.UtcNow;
            }
            else
            {
                var selection = new PaymentSelection
                {
                    RideId = dto.RideId,
                    PaymentMethod = dto.PaymentMethod,
                    UPIId = dto.UPIId,
                    Status = PaymentSelectionStatus.Selected
                };
                _context.PaymentSelections.Add(selection);
            }

            await _context.SaveChangesAsync();
            return Ok("Payment method selected");
        }

        [HttpGet("selection/{rideId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaymentSelection(Guid rideId)
        {
            var selection = await _context.PaymentSelections
                .FirstOrDefaultAsync(ps => ps.RideId == rideId);

            return Ok(selection);
        }
    }

    public class UPIQRDto
    {
        public Guid RideId { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaymentDto
    {
        public Guid RideId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? UPIId { get; set; }
    }

    public class PaymentSelectionDto
    {
        public Guid RideId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? UPIId { get; set; }
    }
}
