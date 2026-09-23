using System.ComponentModel.DataAnnotations;

namespace finance.online.api.Models.DTO
{
    public class OrganizationMemberCreateRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;
    }
}