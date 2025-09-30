using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.Models;
using RideNowAPP.Services;

namespace RideNowAPI.Services
{
    public class RideService : IRideService
    
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
            Console.WriteLine($"[DEBUG] Getting available rides for location: {location}, vehicleType: {vehicleType}");
            
            // Only get rides that are currently requested (not completed, cancelled, or in progress)
            var rides = await _context.Rides
                .Where(r => r.Status == RideStatus.Requested && r.DriverId == null)
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
            
            Console.WriteLine($"[DEBUG] Found {rides.Count} available ride requests");
            foreach (var ride in rides)
            {
                Console.WriteLine($"[DEBUG] Available Ride {ride.RideId}: {ride.CustomerName} - {ride.PickupLocation} -> {ride.DropLocation}, Requested: {ride.RequestedAt}");
            }
            
            return rides;
        }


        
    }
}
