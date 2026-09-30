namespace finance.online.api.Models;

public enum MemberRole
{
    Owner,
    Accountant,
    Member
}

public class Member
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; } = null!;

    public string OrganizationId { get; set; } = null!;

    public MemberRole Role { get; set; } = MemberRole.Member;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public AppUser User { get; set; } = null!;

    public Organization Organization { get; set; } = null!;
}