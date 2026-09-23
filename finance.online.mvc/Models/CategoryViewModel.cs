using finance.online.mvc.Models.DTO;

namespace finance.online.mvc.Models
{
    public class CategoriesViewModel
    {
        public bool HasOrganizations { get; set; }
        public OrganizationResponseDto? ActiveOrganization { get; set; }
        public List<CategoryResponseDto> Categories { get; set; } = new();
    }
}