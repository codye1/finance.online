using finance.online.mvc.Models.DTO;

namespace finance.online.mvc.Models
{
    public class SettingsViewModel
    {
        public OrganizationResponseDto? ActiveOrganization { get; set; }
        public List<OrganizationMemberResponseDto> Members { get; set; } = new();
        public bool HasOrganizations => ActiveOrganization is not null;
    }
}
