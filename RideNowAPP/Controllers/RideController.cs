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

            var ride = await _rideService.CreateRideRequest(
                userId, user.Name, dto.PickupLocation, dto.DropLocation,
                dto.PickupLatitude, dto.PickupLongitude, dto.DropLatitude, dto.DropLongitude,
                dto.VehicleType);

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
            var ride = await _context.Rides.FindAsync(rideId);

            if (ride == null || ride.Status != RideStatus.Requested)
                return BadRequest("Ride not available");

            ride.DriverId = driverId;
            ride.Status = RideStatus.Accepted;
            ride.AcceptedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
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

            if (ride == null || ride.Status != RideStatus.InProgress)
                return BadRequest("Ride cannot be completed");

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
                ride.Status,
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
            var ride = await _context.Rides
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.DriverId == driverId &&
                    (r.Status == RideStatus.Accepted || r.Status == RideStatus.InProgress));

            if (ride == null) return Ok(null);

            return Ok(new {
                ride.RideId,
                ride.CustomerName,
                ride.PickupLocation,
                ride.DropLocation,
                ride.Distance,
                ride.Fare,
                ride.VehicleType,
                ride.Status,
                ride.OTP,
                ride.RequestedAt,
                ride.AcceptedAt,
                CustomerPhone = ride.User?.Phone
            });
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
