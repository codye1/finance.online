using Microsoft.AspNetCore.Identity;

namespace finance.online.api.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
