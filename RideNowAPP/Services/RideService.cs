using RideNowAPI.Data;
using RideNowAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace RideNowAPI.Services
{
    public class RideService
    {
        private readonly RideNowDbContext _context;
        private readonly LocationService _locationService;

        public RideService(RideNowDbContext context, LocationService locationService)
        {
            _context = context;
            _locationService = locationService;
        }

        public string GenerateOTP()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }

        public async Task<Ride> CreateRideRequest(Guid userId, string customerName,
            string pickupLocation, string dropLocation, decimal pickupLat, decimal pickupLng,
            decimal dropLat, decimal dropLng, string vehicleType)
        {
            var distance = _locationService.CalculateDistance(pickupLat, pickupLng, dropLat, dropLng);
            var fare = _locationService.CalculateFare(distance, vehicleType);

            var ride = new Ride
            {
                UserId = userId,
                CustomerName = customerName,
                PickupLocation = pickupLocation,
                DropLocation = dropLocation,
                PickupLatitude = pickupLat,
                PickupLongitude = pickupLng,
                DropLatitude = dropLat,
                DropLongitude = dropLng,
                Distance = (decimal)distance,
                Fare = fare,
                VehicleType = vehicleType,
                OTP = GenerateOTP(),
                Status = RideStatus.Requested
            };

            _context.Rides.Add(ride);
            await _context.SaveChangesAsync();
            return ride;
        }

        public async Task<List<Ride>> GetAvailableRides(string location, string vehicleType)
        {
            return await _context.Rides
                .Where(r => r.Status == RideStatus.Requested &&
                           r.VehicleType == vehicleType)
                .Include(r => r.User)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }
    }
}
