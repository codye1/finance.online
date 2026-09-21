using finance.online.api.Models;

namespace finance.online.api.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(AppUser user, IList<string> roles);
        string GenerateRefreshToken();
        DateTime AccessTokenExpiry { get; }
    }
}
