using System.ComponentModel.DataAnnotations;

namespace finance.online.api.Models.DTO
{
    public class OrganizationMemberUpdateRequestDto
    {
        [Required]
        public MemberRole? Role { get; set; }
    }
}