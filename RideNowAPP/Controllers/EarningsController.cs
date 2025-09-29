using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.Models;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EarningsController : ControllerBase
    {
        private readonly RideNowDbContext _context;

        public EarningsController(RideNowDbContext context)
        {
            _context = context;
        }

        [HttpGet("driver/{driverIdentifier}")]
        public async Task<ActionResult<object>> GetDriverEarnings(string driverIdentifier)
        {
            try
            {
                Guid driverId;
                
                // Try to parse as GUID first, if that fails, treat as email
                if (!Guid.TryParse(driverIdentifier, out driverId))
                {
                    // Find driver by email
                    var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Email == driverIdentifier);
                    if (driver == null)
                    {
                        return NotFound(new { message = "Driver not found" });
                    }
                    driverId = driver.DriverId;
                }

                var earnings = await _context.DriverEarnings
                    .Include(e => e.Ride)
                    .ThenInclude(r => r.User)
                    .Where(e => e.DriverId == driverId)
                    .OrderByDescending(e => e.Date)
                    .Select(e => new
                    {
                        e.EarningId,
                        e.RideId,
                        e.Date,
                        e.Fare,
                        e.PaymentMethod,
                        e.Status,
                        CustomerName = e.Ride.User != null ? e.Ride.User.Name : "Unknown",
                        PickupLocation = e.Ride.PickupLocation,
                        DropLocation = e.Ride.DropLocation
                    })
                    .ToListAsync();

                var totalEarnings = earnings.Sum(e => e.Fare);

                return Ok(new
                {
                    earnings,
                    totalEarnings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch earnings", error = ex.Message });
            }
        }
    }
}