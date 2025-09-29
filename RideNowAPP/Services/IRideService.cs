using RideNowAPI.Models;

namespace RideNowAPP.Services
{
    public interface IRideService
    {
         Task<List<Ride>> GetAvailableRides(string location, string vehicleType);
        Task<Ride> CreateRideRequest(Guid userId, string customerName,
            string pickupLocation, string dropLocation, decimal pickupLat, decimal pickupLng,
            decimal dropLat, decimal dropLng, string vehicleType);

        //Task<Ride?> FinishTripAsync(Guid rideId, Guid driverId);

    }
}
