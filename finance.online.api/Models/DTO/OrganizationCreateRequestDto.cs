using System.ComponentModel.DataAnnotations;

namespace finance.online.api.Models.DTO
{
    public class OrganizationCreateRequestDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = null!;

        public List<string> ParticipantUserIds { get; set; } = new();
    }
}