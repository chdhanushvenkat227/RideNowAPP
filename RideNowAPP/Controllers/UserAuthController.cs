using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.DTOs;
using RideNowAPI.Models;
using RideNowAPI.Services;
using BCrypt.Net;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/auth/user")]
    public class UserAuthController : ControllerBase
    {
        private readonly RideNowDbContext _context;
        private readonly JwtService _jwtService;

        public UserAuthController(RideNowDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(UserRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already exists");

            if (await _context.Users.AnyAsync(u => u.Phone == dto.Phone))
                return BadRequest("Phone number already exists");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Gender = dto.Gender,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user.UserId, user.Email, "User");

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = "User"
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(UserLoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return BadRequest("Invalid email or password");

            var token = _jwtService.GenerateToken(user.UserId, user.Email, "User");

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = "User"
            });
        }
    }
}
