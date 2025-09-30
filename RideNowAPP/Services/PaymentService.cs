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
            Console.WriteLine($"[DEBUG] Processing payment for ride {rideId}, amount {amount}, method {method}");
            
            var ride = await _context.Rides.FindAsync(rideId);
            if (ride == null)
                throw new InvalidOperationException($"Ride {rideId} not found");
            
            Console.WriteLine($"[DEBUG] Ride found: {ride.RideId}, Status: {ride.Status}, DriverId: {ride.DriverId}");

            // Check if payment already exists
            var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.RideId == rideId);
            if (existingPayment != null)
            {
                Console.WriteLine($"[DEBUG] Payment already exists for ride {rideId}, returning existing payment");
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

            // Only create earnings record if driver is assigned
            if (ride.DriverId != null)
            {
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
                    Console.WriteLine($"[DEBUG] Created earnings record for driver {ride.DriverId}");
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] No driver assigned to ride {rideId}, skipping earnings record");
            }

            _context.Payments.Add(payment);
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Payment processed successfully for ride {rideId}");
                return payment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to save payment for ride {rideId}: {ex.Message}");
                throw new InvalidOperationException($"Failed to process payment: {ex.Message}");
            }
        }
    }
}
