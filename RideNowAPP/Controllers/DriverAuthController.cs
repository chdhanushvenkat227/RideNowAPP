using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideNowAPI.DTOs;
using RideNowAPI.Services;
using System.Security.Claims;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/auth/driver")]
    public class DriverAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IDriverService _driverService;

        public DriverAuthController(IAuthService authService, IDriverService driverService)
        {
            _authService = authService;
            _driverService = driverService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(DriverRegisterDto dto)
        {
            try
            {
                var result = await _authService.RegisterDriverAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(DriverLoginDto dto)
        {
            try
            {
                var result = await _authService.LoginDriverAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("preferences")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> SetPreferences(DriverPreferencesDto dto)
        {
            try
            {
                var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _driverService.SetDriverPreferencesAsync(driverId, dto);
                return Ok("Preferences updated successfully");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("complete-profile")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> CompleteProfile(DriverCompleteProfileDto dto)
        {
            try
            {
                var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _driverService.CompleteDriverProfileAsync(driverId, dto);
                return Ok("Profile completed successfully");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("status/{driverId}")]
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

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto dto)
        {
            try
            {
                var resetToken = await _authService.ForgotPasswordDriverAsync(dto);
                Console.WriteLine($"Reset token for {dto.Email}: {resetToken}");
                return Ok(new { message = "Reset token sent to your email", token = resetToken });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            try
            {
                await _authService.ResetPasswordDriverAsync(dto);
                return Ok(new { message = "Password reset successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
