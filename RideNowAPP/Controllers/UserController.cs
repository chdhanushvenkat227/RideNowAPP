using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.Models;
using System.Security.Claims;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "User")]
    public class UserController : ControllerBase
    {
        private readonly RideNowDbContext _context;

        public UserController(RideNowDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                user.UserId,
                user.Name,
                user.Email,
                user.Phone,
                user.Gender,
                user.CreatedAt
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound("User not found");

            user.Name = dto.Name;
            user.Phone = dto.Phone;
            user.Gender = dto.Gender;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Profile updated successfully");
        }

        [HttpGet("ride-history")]
        public async Task<IActionResult> GetRideHistory()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var rides = await _context.Rides
                .Where(r => r.UserId == userId)
                .Include(r => r.Driver)
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
                    DriverName = r.Driver != null ? r.Driver.Name : "Not assigned",
                    PaymentStatus = r.Payment != null ? r.Payment.Status.ToString() : "Pending"
                })
                .ToListAsync();

            return Ok(rides);
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var totalRides = await _context.Rides.CountAsync(r => r.UserId == userId);
            var completedRides = await _context.Rides.CountAsync(r => r.UserId == userId && r.Status == RideStatus.Completed);
            var totalSpent = await _context.Rides
                .Where(r => r.UserId == userId && r.Status == RideStatus.Completed)
                .SumAsync(r => r.Fare);

            return Ok(new
            {
                totalRides,
                completedRides,
                totalSpent,
                averageRating = 4.5
            });
        }


        [HttpDelete("delete-users/{id}")]
        public async Task<IActionResult> DeleteUsers(int id)
        {
            // Get the current user's ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized("User ID claim not found.");
            }

            var currentUserId = Guid.Parse(userIdClaim);

            // Find the user to delete
            var userToDelete = await _context.Users.FindAsync(id);
            if (userToDelete == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

           

            // Delete the user
            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync();

            return Ok($"User with ID {id} has been deleted.");
        }

    }

    public class UpdateUserProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
    }
}
