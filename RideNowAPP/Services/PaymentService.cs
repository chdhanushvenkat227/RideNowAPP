using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.Models;
using RideNowAPP.Services;

namespace RideNowAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly RideNowDbContext _context;

        public PaymentService(RideNowDbContext context)
        {
            _context = context;
        }

        public string GenerateUPIQR(string driverName, decimal amount)
        {
            var upiId = $"pay.{driverName.ToLower().Replace(" ", "")}@okhdfcbank";
            return $"upi://pay?pa={upiId}&pn={driverName}&am={amount}&cu=INR";
        }

        public async Task<Payment> ProcessPayment(Guid rideId, decimal amount,
            PaymentMethod method, string? upiId = null)
        {
            var ride = await _context.Rides.FindAsync(rideId);
            if (ride?.DriverId == null)
                throw new InvalidOperationException("Ride or driver not found");

            // Check if payment already exists
            var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.RideId == rideId);
            if (existingPayment != null)
            {
                Console.WriteLine($"[DEBUG] Payment already exists for ride {rideId}");
                return existingPayment;
            }

            var payment = new Payment
            {
                RideId = rideId,
                Amount = amount,
                PaymentMethod = method,
                Status = PaymentStatus.Completed,
                TransactionId = Guid.NewGuid().ToString("N")[..10],
                UPIId = upiId
            };

            // Check if earnings record already exists
            var existingEarning = await _context.DriverEarnings.FirstOrDefaultAsync(e => e.RideId == rideId);
            if (existingEarning == null)
            {
                var earning = new DriverEarnings
                {
                    DriverId = ride.DriverId.Value,
                    RideId = rideId,
                    Fare = amount,
                    PaymentMethod = method.ToString(),
                    Status = "Received"
                };
                _context.DriverEarnings.Add(earning);
            }

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }
    }
}
