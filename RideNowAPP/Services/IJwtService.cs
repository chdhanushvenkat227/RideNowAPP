using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace RideNowAPP.Services
{
    public interface IJwtService
    {
        string GenerateToken(Guid userId, string email, string role);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        bool IsTokenExpired(string token);
        void RotateKeys();
        IEnumerable<SecurityKey> GetValidationKeys();
    }
}
