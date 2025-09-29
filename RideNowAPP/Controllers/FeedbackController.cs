using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.Models;
using RideNowAPI.DTO;
using System.Security.Claims;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly RideNowDbContext _context;

        public FeedbackController(RideNowDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackDto dto)
        {
            var ride = await _context.Rides.FindAsync(dto.RideId);
            if (ride == null || ride.DriverId == null)
                return BadRequest("Invalid ride");

            var existingFeedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.RideId == dto.RideId);
            
            if (existingFeedback != null)
                return BadRequest("Feedback already submitted for this ride");

            var feedback = new Feedback
            {
                RideId = dto.RideId,
                DriverId = ride.DriverId.Value,
                UserId = ride.UserId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return Ok("Feedback submitted successfully");
        }

        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetDriverFeedback(Guid driverId)
        {
            var feedbacks = await _context.Feedbacks
                .Where(f => f.DriverId == driverId)
                .Include(f => f.User)
                .Include(f => f.Ride)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new {
                    f.FeedbackId,
                    f.Rating,
                    f.Comment,
                    f.CreatedAt,
                    CustomerName = f.User.Name,
                    RideDetails = new {
                        f.Ride.PickupLocation,
                        f.Ride.DropLocation,
                        f.Ride.CompletedAt
                    }
                })
                .ToListAsync();

            var averageRating = feedbacks.Any() ? feedbacks.Average(f => f.Rating) : 0;

            return Ok(new { feedbacks, averageRating });
        }

        [HttpGet("ride/{rideId}")]
        public async Task<IActionResult> GetRideFeedback(Guid rideId)
        {
            var feedback = await _context.Feedbacks
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.RideId == rideId);

            return Ok(feedback);
        }

        [HttpGet("driver/{driverId}/earnings")]
        public async Task<IActionResult> GetDriverEarningsWithFeedback(Guid driverId)
        {
            var earnings = await _context.DriverEarnings
                .Where(de => de.DriverId == driverId)
                .Include(de => de.Ride)
                .Select(de => new {
                    de.EarningId,
                    de.Fare,
                    de.Date,
                    RideId = de.Ride.RideId,
                    RideDetails = new {
                        de.Ride.PickupLocation,
                        de.Ride.DropLocation,
                        de.Ride.CompletedAt
                    }
                })
                .OrderByDescending(de => de.Date)
                .ToListAsync();

            var rideIds = earnings.Select(e => e.RideId).ToList();
            var feedbacks = await _context.Feedbacks
                .Where(f => rideIds.Contains(f.RideId))
                .Include(f => f.User)
                .Select(f => new {
                    f.RideId,
                    f.Rating,
                    f.Comment,
                    CustomerName = f.User.Name,
                    f.CreatedAt
                })
                .ToListAsync();

            var result = earnings.Select(e => new {
                e.EarningId,
                e.Fare,
                e.Date,
                e.RideId,
                e.RideDetails,
                Feedback = feedbacks.FirstOrDefault(f => f.RideId == e.RideId)
            });

            var totalEarnings = earnings.Sum(e => e.Fare);
            var averageRating = feedbacks.Any() ? feedbacks.Average(f => f.Rating) : 0;

            return Ok(new { earnings = result, totalEarnings, averageRating });
        }
    }

}