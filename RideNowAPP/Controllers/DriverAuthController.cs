using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.DTOs;
using RideNowAPI.Models;
using RideNowAPI.Services;
using System.Security.Claims;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/auth/driver")]
    public class DriverAuthController : ControllerBase
    {
        private readonly RideNowDbContext _context;
        private readonly JwtService _jwtService;

        public DriverAuthController(RideNowDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(DriverRegisterDto dto)
        {
            if (await _context.Drivers.AnyAsync(d => d.Email == dto.Email))
                return BadRequest("Email already exists");

            if (await _context.Drivers.AnyAsync(d => d.Phone == dto.Phone))
                return BadRequest("Phone number already exists");

            var driver = new Driver
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsActive = false
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(driver.DriverId, driver.Email, "Driver");

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = driver.DriverId,
                Name = driver.Name,
                Email = driver.Email,
                Role = "Driver"
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(DriverLoginDto dto)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Email == dto.Email);

            if (driver == null || !BCrypt.Net.BCrypt.Verify(dto.Password, driver.PasswordHash))
                return BadRequest("Invalid email or password");

            var token = _jwtService.GenerateToken(driver.DriverId, driver.Email, "Driver");

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = driver.DriverId,
                Name = driver.Name,
                Email = driver.Email,
                Role = "Driver"
            });
        }

        [HttpPut("preferences")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> SetPreferences(DriverPreferencesDto dto)
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var driver = await _context.Drivers.FindAsync(driverId);

            if (driver == null)
                return NotFound("Driver not found");

            driver.Location = dto.Location;
            driver.VehicleType = dto.VehicleType;
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Preferences updated successfully");
        }

        [HttpPut("complete-profile")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> CompleteProfile(DriverCompleteProfileDto dto)
        {
            var driverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var driver = await _context.Drivers.FindAsync(driverId);

            if (driver == null)
                return NotFound("Driver not found");

            if (await _context.Drivers.AnyAsync(d => d.LicenseNumber == dto.LicenseNumber && d.DriverId != driverId))
                return BadRequest("License number already exists");

            driver.Gender = dto.Gender;
            driver.LicenseNumber = dto.LicenseNumber;
            driver.LicenseExpiryDate = dto.LicenseExpiryDate;
            driver.BloodGroup = dto.BloodGroup;
            driver.Address = dto.Address;
            driver.IsActive = true;
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Profile completed successfully");
        }
    }
}
