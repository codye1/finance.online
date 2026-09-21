namespace finance.online.api.Models.DTO
{
    public class OrganizationMemberResponseDto
    {
        public string Id { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public string? Email { get; set; }

        public string Role { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public bool IsMe { get; set; }

        public bool IsOwner { get; set; }
    }
}