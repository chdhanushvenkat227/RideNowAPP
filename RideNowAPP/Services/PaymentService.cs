using RideNowAPI.Data;
using RideNowAPI.Models;

namespace RideNowAPI.Services
{
    public class PaymentService
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
            var payment = new Payment
            {
                RideId = rideId,
                Amount = amount,
                PaymentMethod = method,
                Status = PaymentStatus.Completed,
                TransactionId = Guid.NewGuid().ToString("N")[..10],
                UPIId = upiId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }
    }
}
