using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.DTOs;
using RideNowAPI.Models;

namespace RideNowAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly RideNowDbContext _context;
        private readonly JwtService _jwtService;

        public AuthService(RideNowDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AuthResponseDto> RegisterUserAsync(UserRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email already exists");

            if (await _context.Users.AnyAsync(u => u.Phone == dto.Phone))
                throw new InvalidOperationException("Phone number already exists");

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
            var refreshToken = _jwtService.GenerateRefreshToken();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = "User"
            };
        }

        public async Task<AuthResponseDto> LoginUserAsync(UserLoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password");

            var token = _jwtService.GenerateToken(user.UserId, user.Email, "User");
            var refreshToken = _jwtService.GenerateRefreshToken();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = "User"
            };
        }

        public async Task<string> ForgotPasswordUserAsync(ForgotPasswordRequestDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user == null)
                throw new InvalidOperationException("No user found with this email address");

            var resetToken = new Random().Next(100000, 999999).ToString();
            user.ResetToken = resetToken;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return resetToken;
        }

        public async Task<bool> ResetPasswordUserAsync(ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || user.ResetToken != dto.Token || user.ResetTokenExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("Invalid or expired reset token");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResponseDto> RegisterDriverAsync(DriverRegisterDto dto)
        {
            if (await _context.Drivers.AnyAsync(d => d.Email == dto.Email))
                throw new InvalidOperationException("Email already exists");

            if (await _context.Drivers.AnyAsync(d => d.Phone == dto.Phone))
                throw new InvalidOperationException("Phone number already exists");

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
            var refreshToken = _jwtService.GenerateRefreshToken();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                UserId = driver.DriverId,
                Name = driver.Name,
                Email = driver.Email,
                Phone = driver.Phone,
                Role = "Driver"
            };
        }

        public async Task<AuthResponseDto> LoginDriverAsync(DriverLoginDto dto)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Email == dto.Email);

            if (driver == null || !BCrypt.Net.BCrypt.Verify(dto.Password, driver.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password");

            var token = _jwtService.GenerateToken(driver.DriverId, driver.Email, "Driver");
            var refreshToken = _jwtService.GenerateRefreshToken();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                UserId = driver.DriverId,
                Name = driver.Name,
                Email = driver.Email,
                Phone = driver.Phone,
                Role = "Driver"
            };
        }

        public async Task<string> ForgotPasswordDriverAsync(ForgotPasswordRequestDto dto)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Email.ToLower() == dto.Email.ToLower());
            if (driver == null)
                throw new InvalidOperationException("No driver found with this email address");

            var resetToken = new Random().Next(100000, 999999).ToString();
            driver.ResetToken = resetToken;
            driver.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return resetToken;
        }

        public async Task<bool> ResetPasswordDriverAsync(ResetPasswordDto dto)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Email == dto.Email);
            if (driver == null || driver.ResetToken != dto.Token || driver.ResetTokenExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("Invalid or expired reset token");

            driver.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            driver.ResetToken = null;
            driver.ResetTokenExpiry = null;
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}