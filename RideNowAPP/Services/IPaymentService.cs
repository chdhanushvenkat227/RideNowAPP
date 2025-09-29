using RideNowAPI.Models;

namespace RideNowAPP.Services
{
    public interface IPaymentService
    {
        string GenerateUPIQR(string driverName, decimal amount);

        Task<Payment> ProcessPayment(Guid rideId, decimal amount, PaymentMethod method, string? upiId = null);
    }
}
