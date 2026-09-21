using System.ComponentModel.DataAnnotations;

namespace finance.online.api.Models.DTO
{
    public class OrganizationUpdateRequestDto
    {
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}