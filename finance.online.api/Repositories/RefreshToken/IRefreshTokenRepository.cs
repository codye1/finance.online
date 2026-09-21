using finance.online.api.Models;

namespace finance.online.api.Repositories.RefreshTokenRepository
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        void Add(RefreshToken refreshToken);
        void Update(RefreshToken refreshToken);
        Task SaveChangesAsync();
    }
}
