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
            try
            {
                var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var result = await _driverService.GetDriverProfileAsync(driverId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateDriverProfileDto dto)
        {
            try
            {
                var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _driverService.UpdateDriverProfileAsync(driverId, dto);
                return Ok("Profile updated successfully");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
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
        [AllowAnonymous]
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
        
        [HttpPost("status/{driverId}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateDriverStatusByIdPost(Guid driverId, [FromBody] UpdateDriverStatusDto dto)
        {
            try
            {
                Console.WriteLine($"[DEBUG] POST: Updating driver {driverId} status to {dto.Status}");
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
        public IActionResult GetEarnings()
        {
            return StatusCode(501, "Earnings endpoint not implemented in service layer");
        }

        [HttpGet("ride-history")]
        public IActionResult GetRideHistory()
        {
            return StatusCode(501, "Ride history endpoint not implemented in service layer");
        }

        [HttpGet("dashboard-stats")]
        public IActionResult GetDashboardStats()
        {
            return StatusCode(501, "Dashboard stats endpoint not implemented in service layer");
        }

        [HttpPost("feedback")]
        public IActionResult CreateFeedback([FromBody] CreateFeedbackDto dto)
        {
            return StatusCode(501, "Feedback creation not implemented in service layer");
        }

        [HttpGet("feedback")]
        public IActionResult GetFeedback()
        {
            return StatusCode(501, "Feedback retrieval not implemented in service layer");
        }
    }


}
