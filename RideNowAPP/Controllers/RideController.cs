using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.Models;
using RideNowAPI.Services;
using System.Security.Claims;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/rides")]
    [Authorize]
    public class RideController : ControllerBase
    {
        private readonly RideNowDbContext _context;
        private readonly RideService _rideService;

        public RideController(RideNowDbContext context, RideService rideService)
        {
            _context = context;
            _rideService = rideService;
        }

        [HttpPost("request")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> RequestRide([FromBody] RideRequestDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _context.Users.FindAsync(userId);
            
            Console.WriteLine($"[DEBUG] New ride request from user {user.Name}: {dto.PickupLocation} -> {dto.DropLocation}");

            var ride = await _rideService.CreateRideRequest(
                userId, user.Name, dto.PickupLocation, dto.DropLocation,
                dto.PickupLatitude, dto.PickupLongitude, dto.DropLatitude, dto.DropLongitude,
                dto.VehicleType);
            
            Console.WriteLine($"[DEBUG] Created ride {ride.RideId} with status {ride.Status}");

            return Ok(new
            {
                rideId = ride.RideId,
                otp = ride.OTP,
                fare = ride.Fare,
                distance = ride.Distance
            });
        }

        [HttpGet("available")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> GetAvailableRides([FromQuery] string location, [FromQuery] string vehicleType)
        {
            var rides = await _rideService.GetAvailableRides(location, vehicleType);
            return Ok(rides.Select(r => new {
                r.RideId,
                r.CustomerName,
                r.PickupLocation,
                r.DropLocation,
                r.Distance,
                r.Fare,
                r.RequestedAt
            }));
        }

        [HttpPut("{rideId}/accept")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> AcceptRide(Guid rideId)
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            // First, complete any old InProgress rides for this driver
            var oldRides = await _context.Rides
                .Where(r => r.DriverId == driverId && 
                       (r.Status == RideStatus.InProgress || r.Status == RideStatus.Accepted))
                .ToListAsync();
            
            foreach (var oldRide in oldRides)
            {
                Console.WriteLine($"[DEBUG] Force completing old ride {oldRide.RideId}");
                oldRide.Status = RideStatus.Completed;
                oldRide.CompletedAt = DateTime.UtcNow;
            }
            
            var ride = await _context.Rides.FindAsync(rideId);
            
            Console.WriteLine($"[DEBUG] Driver {driverId} attempting to accept ride {rideId}");

            if (ride == null || ride.Status != RideStatus.Requested)
            {
                Console.WriteLine($"[DEBUG] Ride not available. Ride exists: {ride != null}, Status: {ride?.Status}");
                return BadRequest("Ride not available");
            }
            
            Console.WriteLine($"[DEBUG] Accepting ride: {ride.PickupLocation} -> {ride.DropLocation}");

            ride.DriverId = driverId;
            ride.Status = RideStatus.Accepted;
            ride.AcceptedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] Ride {rideId} accepted successfully by driver {driverId}");
            return Ok("Ride accepted successfully");
        }

        [HttpPost("{rideId}/verify-otp")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> VerifyOTP(Guid rideId, [FromBody] OTPVerifyDto dto)
        {
            var ride = await _context.Rides.FindAsync(rideId);

            if (ride == null || ride.OTP != dto.OTP)
                return BadRequest("Invalid OTP");

            ride.Status = RideStatus.InProgress;
            ride.StartedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Ride started successfully");
        }

        [HttpPut("{rideId}/complete")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> CompleteRide(Guid rideId)
        {
            var ride = await _context.Rides.FindAsync(rideId);

            if (ride == null)
                return BadRequest("Ride not found");

            if (ride.Status != RideStatus.InProgress && ride.Status != RideStatus.Accepted)
                return BadRequest($"Ride cannot be completed. Current status: {ride.Status}");

            // If ride was never started, mark it as started now
            if (ride.StartedAt == null)
                ride.StartedAt = DateTime.UtcNow;

            ride.Status = RideStatus.Completed;
            ride.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Ride completed successfully");
        }

        [HttpGet("user/current")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetCurrentUserRide()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var ride = await _context.Rides
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.UserId == userId &&
                    (r.Status == RideStatus.Requested || r.Status == RideStatus.Accepted || r.Status == RideStatus.InProgress));

            if (ride == null) return Ok(null);

            return Ok(new {
                ride.RideId,
                ride.CustomerName,
                ride.PickupLocation,
                ride.DropLocation,
                ride.Distance,
                ride.Fare,
                ride.VehicleType,
                Status = ride.Status.ToString(),
                ride.OTP,
                ride.RequestedAt,
                ride.AcceptedAt,
                DriverName = ride.Driver?.Name
            });
        }

        [HttpGet("driver/current")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> GetCurrentDriverRide()
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            Console.WriteLine($"[DEBUG] GetCurrentDriverRide called for driverId: {driverId}");
            
            // Debug: Check all rides for this driver
            var allRides = await _context.Rides
                .Include(r => r.User)
                .Where(r => r.DriverId == driverId)
                .OrderByDescending(r => r.RequestedAt)
                .Take(10)
                .ToListAsync();
            
            Console.WriteLine($"[DEBUG] Found {allRides.Count} total rides for driver");
            foreach (var ride in allRides)
            {
                Console.WriteLine($"[DEBUG] Ride {ride.RideId}: Status={ride.Status} ({(int)ride.Status}), {ride.PickupLocation} -> {ride.DropLocation}");
            }
            
            // Only return active rides (Accepted or InProgress) - STRICT FILTER
            var activeRides = await _context.Rides
                .Include(r => r.User)
                .Where(r => r.DriverId == driverId)
                .ToListAsync();
            
            var activeRide = activeRides
                .Where(r => r.Status == RideStatus.Accepted || r.Status == RideStatus.InProgress)
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefault();
            
            if (activeRide != null && (activeRide.Status == RideStatus.Accepted || activeRide.Status == RideStatus.InProgress))
            {
                Console.WriteLine($"[DEBUG] ✅ Returning active ride: {activeRide.RideId} - Status: {activeRide.Status}");
                return Ok(new {
                    rideId = activeRide.RideId,
                    customerName = activeRide.CustomerName,
                    pickupLocation = activeRide.PickupLocation,
                    dropLocation = activeRide.DropLocation,
                    distance = activeRide.Distance,
                    fare = activeRide.Fare,
                    vehicleType = activeRide.VehicleType,
                    status = activeRide.Status.ToString(),
                    otp = activeRide.OTP,
                    requestedAt = activeRide.RequestedAt,
                    acceptedAt = activeRide.AcceptedAt,
                    startedAt = activeRide.StartedAt,
                    user = new { name = activeRide.User?.Name, phone = activeRide.User?.Phone }
                });
            }
            else if (activeRide != null)
            {
                Console.WriteLine($"[DEBUG] ❌ Ignoring completed ride: {activeRide.RideId} - Status: {activeRide.Status}");
            }
            
            Console.WriteLine($"[DEBUG] ❌ No active rides found for driver {driverId}");
            return Ok(null);
        }

        
    

    }
    public class RideRequestDto
    {
        public string PickupLocation { get; set; } = string.Empty;
        public string DropLocation { get; set; } = string.Empty;
        public decimal PickupLatitude { get; set; }
        public decimal PickupLongitude { get; set; }
        public decimal DropLatitude { get; set; }
        public decimal DropLongitude { get; set; }
        public string VehicleType { get; set; } = string.Empty;
    }

    public class OTPVerifyDto
    {
        public string OTP { get; set; } = string.Empty;
    }
}
