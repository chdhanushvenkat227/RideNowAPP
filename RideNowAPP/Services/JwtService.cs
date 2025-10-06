using Microsoft.IdentityModel.Tokens;
using RideNowAPP.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RideNowAPI.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly List<SecurityKey> _signingKeys;
        private int _currentKeyIndex = 0;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            _signingKeys = InitializeKeys();
        }

        private List<SecurityKey> InitializeKeys()
        {
            var keys = new List<SecurityKey>();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            
            // Primary key
            var primaryKey = jwtSettings["SecretKey"];
            keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(primaryKey)));
            
            // Secondary key for rotation
            var secondaryKey = jwtSettings["SecondaryKey"];
            if (!string.IsNullOrEmpty(secondaryKey))
            {
                keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secondaryKey)));
            }
            
            return keys;
        }

        public string GenerateToken(Guid userId, string email, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryInMinutes = int.Parse(jwtSettings["ExpiryInMinutes"]);

            var key = _signingKeys[_currentKeyIndex];
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("kid", _currentKeyIndex.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            
            // Try validating with all available keys
            foreach (var key in _signingKeys)
            {
                try
                {
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                    return principal;
                }
                catch
                {
                    continue; // Try next key
                }
            }
            
            return null;
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                return jsonToken.ValidTo < DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        public void RotateKeys()
        {
            _currentKeyIndex = (_currentKeyIndex + 1) % _signingKeys.Count;
        }

        public IEnumerable<SecurityKey> GetValidationKeys()
        {
            return _signingKeys;
        }
    }
}
