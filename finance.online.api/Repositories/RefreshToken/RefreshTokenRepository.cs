using finance.online.api.Data;
using finance.online.api.Models;
using finance.online.api.Repositories.RefreshTokenRepository;
using Microsoft.EntityFrameworkCore;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly FinanceOnlineDbContext _context;

    public RefreshTokenRepository(FinanceOnlineDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public void Add(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}