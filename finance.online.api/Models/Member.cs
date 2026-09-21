namespace finance.online.api.Models;

public class Member
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; } = null!;

    public string OrganizationId { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public AppUser User { get; set; } = null!;

    public Organization Organization { get; set; } = null!;
}