using Microsoft.EntityFrameworkCore;
using RideNowAPI.Data;
using RideNowAPI.DTOs;

namespace RideNowAPI.Services
{
    public class DriverService : IDriverService
    {
        private readonly RideNowDbContext _context;

        public DriverService(RideNowDbContext context)
        {
            _context = context;
        }

        public async Task<DriverStatusDto> GetDriverStatusAsync(Guid driverId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                throw new InvalidOperationException("Driver not found");

            return new DriverStatusDto
            {
                Status = driver.Status.ToString(),
                DriverId = driver.DriverId,
                Name = driver.Name,
                Email = driver.Email
            };
        }

        public async Task<bool> UpdateDriverStatusAsync(Guid driverId, UpdateDriverStatusDto dto)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                throw new InvalidOperationException("Driver not found");

            if (Enum.TryParse<Models.DriverStatus>(dto.Status, out var status))
            {
                driver.Status = status;
                driver.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            throw new ArgumentException("Invalid status value");
        }

        public async Task<bool> SetDriverPreferencesAsync(Guid driverId, DriverPreferencesDto dto)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                throw new InvalidOperationException("Driver not found");

            driver.Location = dto.Location;
            driver.VehicleType = dto.VehicleType;
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteDriverProfileAsync(Guid driverId, DriverCompleteProfileDto dto)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                throw new InvalidOperationException("Driver not found");

            if (await _context.Drivers.AnyAsync(d => d.LicenseNumber == dto.LicenseNumber && d.DriverId != driverId))
                throw new InvalidOperationException("License number already exists");

            driver.Gender = dto.Gender;
            driver.LicenseNumber = dto.LicenseNumber;
            driver.LicenseExpiryDate = dto.LicenseExpiryDate;
            driver.BloodGroup = dto.BloodGroup;
            driver.Address = dto.Address;
            driver.IsActive = true;
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDriverProfileAsync(Guid driverId, UpdateDriverProfileDto dto)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                throw new InvalidOperationException("Driver not found");

            // Check if phone number already exists for another driver
            if (await _context.Drivers.AnyAsync(d => d.Phone == dto.Phone && d.DriverId != driverId))
                throw new InvalidOperationException("Phone number already exists");

            driver.Name = dto.Name;
            driver.Phone = dto.Phone;
            driver.Gender = dto.Gender;
            driver.BloodGroup = dto.BloodGroup;
            driver.Address = dto.Address;
            driver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<DriverProfileDto> GetDriverProfileAsync(Guid driverId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                throw new InvalidOperationException("Driver not found");

            return new DriverProfileDto
            {
                DriverId = driver.DriverId,
                Name = driver.Name,
                Email = driver.Email,
                Phone = driver.Phone,
                Gender = driver.Gender,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiryDate = driver.LicenseExpiryDate ?? DateTime.MinValue,
                BloodGroup = driver.BloodGroup,
                Address = driver.Address,
                Location = driver.Location,
                VehicleType = driver.VehicleType,
                Status = driver.Status.ToString(),
                IsActive = driver.IsActive,
                CreatedAt = driver.CreatedAt
            };
        }
    }
}