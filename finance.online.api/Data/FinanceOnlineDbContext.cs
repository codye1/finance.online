using finance.online.api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace finance.online.api.Data
{
    public class FinanceOnlineDbContext
        : IdentityDbContext<AppUser>
    {
        public FinanceOnlineDbContext(
            DbContextOptions<FinanceOnlineDbContext> options)
            : base(options)
        {
        }
    }
}