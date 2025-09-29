namespace RideNowAPP.Services
{
    public interface IJwtService
    {
        public string GenerateToken(Guid userId, string email, string role);
    }
}
