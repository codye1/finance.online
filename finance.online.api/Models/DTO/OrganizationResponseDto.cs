namespace finance.online.api.Models.DTO
{
    public class OrganizationResponseDto
    {
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string CreatedById { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public int MemberCount { get; set; }

        public bool IsOwner { get; set; }
    }
}