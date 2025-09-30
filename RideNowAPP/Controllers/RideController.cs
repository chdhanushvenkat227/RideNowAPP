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
            
            Console.WriteLine($"[DEBUG] Driver {driverId} attempting to accept ride {rideId}");
            
            // Check if ride is still available (not already accepted)
            var ride = await _context.Rides.FindAsync(rideId);
            
            if (ride == null)
            {
                Console.WriteLine($"[DEBUG] Ride {rideId} not found");
                return BadRequest("Ride not found");
            }
            
            if (ride.Status != RideStatus.Requested || ride.DriverId != null)
            {
                Console.WriteLine($"[DEBUG] Ride not available. Status: {ride.Status}, DriverId: {ride.DriverId}");
                return BadRequest("Ride already accepted by another driver");
            }
            
            // Update driver status to Riding
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver != null)
            {
                driver.Status = Models.DriverStatus.Riding;
                driver.UpdatedAt = DateTime.UtcNow;
            }
            
            // Accept the ride
            ride.DriverId = driverId;
            ride.Status = RideStatus.Accepted;
            ride.AcceptedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] Ride {rideId} accepted successfully by driver {driverId}");
            
            return Ok(new {
                message = "Ride accepted successfully",
                rideId = ride.RideId,
                otp = ride.OTP,
                customerName = ride.CustomerName
            });
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
            
            // First check for active rides
            var activeRide = await _context.Rides
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.UserId == userId &&
                    (r.Status == RideStatus.Requested || r.Status == RideStatus.Accepted || r.Status == RideStatus.InProgress));

            if (activeRide != null)
            {
                Console.WriteLine($"[DEBUG] User {userId} current ride: {activeRide.RideId}, Status: {activeRide.Status}, Driver: {activeRide.Driver?.Name}");
                
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
                    driverName = activeRide.Driver?.Name,
                    driverPhone = activeRide.Driver?.Phone
                });
            }
            
            // If no active ride, check for recently completed rides without payment
            var completedRide = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Payment)
                .Where(r => r.UserId == userId && r.Status == RideStatus.Completed)
                .Where(r => r.Payment == null) // No payment record yet
                .OrderByDescending(r => r.CompletedAt)
                .FirstOrDefaultAsync();

            if (completedRide != null)
            {
                return Ok(new {
                    rideId = completedRide.RideId,
                    customerName = completedRide.CustomerName,
                    pickupLocation = completedRide.PickupLocation,
                    dropLocation = completedRide.DropLocation,
                    distance = completedRide.Distance,
                    fare = completedRide.Fare,
                    vehicleType = completedRide.VehicleType,
                    status = completedRide.Status.ToString(),
                    otp = completedRide.OTP,
                    requestedAt = completedRide.RequestedAt,
                    acceptedAt = completedRide.AcceptedAt,
                    completedAt = completedRide.CompletedAt,
                    driverName = completedRide.Driver?.Name
                });
            }

            return Ok(null);
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
