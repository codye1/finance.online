namespace tests.Mocks.Models;

public sealed class FakeMember
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "accountant";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}