using Microsoft.AspNetCore.Mvc;
using RideNowAPI.DTOs;
using RideNowAPI.Services;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserAuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UserAuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(UserRegisterDto dto)
        {
            try
            {
                var result = await _authService.RegisterUserAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(UserLoginDto dto)
        {
            try
            {
                var result = await _authService.LoginUserAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto dto)
        {
            try
            {
                var resetToken = await _authService.ForgotPasswordUserAsync(dto);
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
                await _authService.ResetPasswordUserAsync(dto);
                return Ok(new { message = "Password reset successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
