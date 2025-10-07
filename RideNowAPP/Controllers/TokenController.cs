using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.Services;
using System.Security.Claims;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class TokenController : ControllerBase
    {
        private readonly RideNowDbContext _context;
        private readonly JwtService _jwtService;

        public TokenController(RideNowDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest("Refresh token is required");

            try
            {
                var storedToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && 
                                              !rt.IsRevoked && 
                                              rt.ExpiryDate > DateTime.UtcNow);

                if (storedToken == null)
                    return Unauthorized("Invalid refresh token");

                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(request.AccessToken);
                
                var userId = jsonToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
                var email = jsonToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
                var role = jsonToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
                    return Unauthorized("Invalid token claims");

                // Revoke old refresh token
                storedToken.IsRevoked = true;
                
                // Generate new tokens
                var newAccessToken = _jwtService.GenerateToken(Guid.Parse(userId), email, role);
                var newRefreshToken = _jwtService.GenerateRefreshToken();
                
                // Store new refresh token
                _context.RefreshTokens.Add(new RideNowAPP.Models.RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = Guid.Parse(userId),
                    UserType = role,
                    ExpiryDate = DateTime.UtcNow.AddDays(1),
                    CreatedAt = DateTime.UtcNow
                });
                
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    accessToken = newAccessToken,
                    refreshToken = newRefreshToken,
                    expiresIn = 60
                });
            }
            catch
            {
                return Unauthorized("Invalid refresh token format");
            }
        }

        [HttpPost("revoke")]
        [Authorize]
        public IActionResult RevokeToken([FromBody] RevokeTokenRequest request)
        {
            return Ok(new { message = "Token revoked successfully" });
        }

        [HttpPost("validate")]
        public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
        {
            var principal = _jwtService.ValidateToken(request.Token);
            if (principal == null)
                return Unauthorized("Invalid token");

            var isExpired = _jwtService.IsTokenExpired(request.Token);
            
            return Ok(new
            {
                valid = !isExpired,
                expired = isExpired,
                userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                email = principal.FindFirst(ClaimTypes.Email)?.Value,
                role = principal.FindFirst(ClaimTypes.Role)?.Value
            });
        }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
    }

    public class RevokeTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ValidateTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}