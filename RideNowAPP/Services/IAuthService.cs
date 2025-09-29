using RideNowAPI.DTOs;

namespace RideNowAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterUserAsync(UserRegisterDto dto);
        Task<AuthResponseDto> LoginUserAsync(UserLoginDto dto);
        Task<string> ForgotPasswordUserAsync(ForgotPasswordRequestDto dto);
        Task<bool> ResetPasswordUserAsync(ResetPasswordDto dto);
        
        Task<AuthResponseDto> RegisterDriverAsync(DriverRegisterDto dto);
        Task<AuthResponseDto> LoginDriverAsync(DriverLoginDto dto);
        Task<string> ForgotPasswordDriverAsync(ForgotPasswordRequestDto dto);
        Task<bool> ResetPasswordDriverAsync(ResetPasswordDto dto);
    }
}