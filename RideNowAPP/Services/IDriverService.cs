using RideNowAPI.DTOs;

namespace RideNowAPI.Services
{
    public interface IDriverService
    {
        Task<DriverStatusDto> GetDriverStatusAsync(Guid driverId);
        Task<bool> UpdateDriverStatusAsync(Guid driverId, UpdateDriverStatusDto dto);
        Task<bool> SetDriverPreferencesAsync(Guid driverId, DriverPreferencesDto dto);
        Task<bool> CompleteDriverProfileAsync(Guid driverId, DriverCompleteProfileDto dto);
        Task<bool> UpdateDriverProfileAsync(Guid driverId, UpdateDriverProfileDto dto);
        Task<DriverProfileDto> GetDriverProfileAsync(Guid driverId);
    }
}