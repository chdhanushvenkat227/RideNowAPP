using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideNowAPI.DTOs;
using RideNowAPI.Services;
using System.Security.Claims;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/drivers")]
    [Authorize(Roles = "Driver")]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;

        public DriverController(IDriverService driverService)
        {
            _driverService = driverService;
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

        [HttpGet("status/{driverId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDriverStatus(Guid driverId)
        {
            try
            {
                var result = await _driverService.GetDriverStatusAsync(driverId);
                return Ok(new { status = result.Status });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateDriverStatusDto dto)
        {
            try
            {
                var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _driverService.UpdateDriverStatusAsync(driverId, dto);
                return Ok("Status updated successfully");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
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

        [HttpPost("feedback")]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackDto dto)
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            var ride = await _context.Rides.FindAsync(dto.RideId);
            if (ride == null || ride.DriverId != driverId)
                return BadRequest("Invalid ride");

            var feedback = new Feedback
            {
                RideId = dto.RideId,
                DriverId = driverId,
                UserId = ride.UserId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return Ok("Feedback saved successfully");
        }

        [HttpGet("feedback")]
        public async Task<IActionResult> GetFeedback()
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            var feedbacks = await _context.Feedbacks
                .Where(f => f.DriverId == driverId)
                .Include(f => f.Ride)
                .Include(f => f.User)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new {
                    f.FeedbackId,
                    f.Rating,
                    f.Comment,
                    f.CreatedAt,
                    RideId = f.Ride.RideId,
                    CustomerName = f.User.Name
                })
                .ToListAsync();

            return Ok(feedbacks);
        }
    }


}
