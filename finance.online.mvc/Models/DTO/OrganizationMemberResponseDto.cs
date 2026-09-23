namespace finance.online.mvc.Models.DTO
{
    public class OrganizationMemberResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsMe { get; set; }
        public bool IsOwner { get; set; }
    }
}
