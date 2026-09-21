using System.ComponentModel.DataAnnotations;

namespace finance.online.api.Models.DTO
{
    public class OrganizationMemberCreateRequestDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;
    }
}