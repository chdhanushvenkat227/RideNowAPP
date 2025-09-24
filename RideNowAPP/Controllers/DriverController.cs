using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.Models;
using System.Security.Claims;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/drivers")]
    [Authorize(Roles = "Driver")]
    public class DriverController : ControllerBase
    {
        private readonly RideNowDbContext _context;

        public DriverController(RideNowDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var driver = await _context.Drivers.FindAsync(driverId);

            if (driver == null)
                return NotFound("Driver not found");

            return Ok(new
            {
                driver.DriverId,
                driver.Name,
                driver.Email,
                driver.Phone,
                driver.Gender,
                driver.LicenseNumber,
                driver.LicenseExpiryDate,
                driver.BloodGroup,
                driver.Address,
                driver.Location,
                driver.VehicleType,
                driver.Status,
                driver.IsActive,
                driver.CreatedAt
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateDriverProfileDto dto)
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var driver = await _context.Drivers.FindAsync(driverId);

            if (driver == null)
                return NotFound("Driver not found");

            driver.Name = dto.Name;
            driver.Phone = dto.Phone;
            driver.Gender = dto.Gender;
            driver.BloodGroup = dto.BloodGroup;
            driver.Address = dto.Address;
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Profile updated successfully");
        }

        [HttpPut("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateDriverStatusDto dto)
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var driver = await _context.Drivers.FindAsync(driverId);

            if (driver == null)
                return NotFound("Driver not found");

            driver.Status = dto.Status;
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Status updated successfully");
        }

        [HttpGet("earnings")]
        public async Task<IActionResult> GetEarnings()
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var earnings = await _context.DriverEarnings
                .Where(de => de.DriverId == driverId)
                .Include(de => de.Ride)
                .OrderByDescending(de => de.Date)
                .Select(de => new {
                    de.EarningId,
                    de.Date,
                    de.Fare,
                    de.PaymentMethod,
                    de.Status,
                    RideDetails = new
                    {
                        de.Ride.PickupLocation,
                        de.Ride.DropLocation,
                        de.Ride.Distance
                    }
                })
                .ToListAsync();

            var totalEarnings = await _context.DriverEarnings
                .Where(de => de.DriverId == driverId)
                .SumAsync(de => de.Fare);

            return Ok(new { earnings, totalEarnings });
        }

        [HttpGet("ride-history")]
        public async Task<IActionResult> GetRideHistory()
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var rides = await _context.Rides
                .Where(r => r.DriverId == driverId)
                .Include(r => r.User)
                .Include(r => r.Payment)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new {
                    r.RideId,
                    r.PickupLocation,
                    r.DropLocation,
                    r.Distance,
                    r.Fare,
                    r.VehicleType,
                    r.Status,
                    r.RequestedAt,
                    r.CompletedAt,
                    CustomerName = r.User.Name,
                    PaymentStatus = r.Payment != null ? r.Payment.Status.ToString() : "Pending"
                })
                .ToListAsync();

            return Ok(rides);
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var totalRides = await _context.Rides.CountAsync(r => r.DriverId == driverId);
            var completedRides = await _context.Rides.CountAsync(r => r.DriverId == driverId && r.Status == RideStatus.Completed);
            var totalEarnings = await _context.DriverEarnings
                .Where(de => de.DriverId == driverId)
                .SumAsync(de => de.Fare);

            return Ok(new
            {
                totalRides,
                completedRides,
                totalEarnings,
                averageRating = 4.7
            });
        }
    }

    public class UpdateDriverProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? BloodGroup { get; set; }
        public string? Address { get; set; }
    }

    public class UpdateDriverStatusDto
    {
        public DriverStatus Status { get; set; }
    }
}
