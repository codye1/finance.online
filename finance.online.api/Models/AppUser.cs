using Azure;
using Microsoft.AspNetCore.Identity;

namespace finance.online.api.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public ICollection<Organization> CreatedOrganizations { get; set; } = new List<Organization>();

        public ICollection<Member> Memberships { get; set; } = new List<Member>();

        public ICollection<OperationModel> CreatedOperations { get; set; } = new List<OperationModel>();
    }
}
