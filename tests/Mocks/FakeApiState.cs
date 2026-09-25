using System.Text.RegularExpressions;
using tests.Mocks.Models;

namespace tests.Mocks;

// =========================================================================
// In-memory state
// =========================================================================

public sealed class FakeApiState
{
    public const string SeedEmail = "newuser@example.com";
    public const string SeedPassword = "Password123!";
    public const string SeedInvitableEmail = "member@example.com";

    public Dictionary<string, string> Users { get; private set; } = new();
    public List<FakeOrg> Orgs { get; private set; } = new();
    public Dictionary<string, string> ActiveOrg { get; private set; } = new();
    public List<(string Method, Regex Path, int Status)> Overrides { get; private set; } = new();
    public string? LastLoginEmail { get; set; }

    public FakeApiState() => Reset();

    public IEnumerable<FakeOrg> OrgsFor(string email) =>
        Orgs.Where(o => o.Members.Any(m => m.Email == email));

    public void Reset()
    {
        Users = new Dictionary<string, string>
        {
            [SeedEmail] = SeedPassword,
            [SeedInvitableEmail] = SeedPassword   // registered, but not a member of any org
        };
        Orgs = new List<FakeOrg>();
        ActiveOrg = new Dictionary<string, string>();
        Overrides = new List<(string, Regex, int)>();
        LastLoginEmail = null;

        var org = new FakeOrg { Id = "org-seed-1", Name = "Test Organization", Description = "Seeded for UI tests" };
        org.Members.Add(new FakeMember { Email = SeedEmail, Role = "owner" });

        var salary = new FakeCategory { Id = "cat-salary", Name = "Salary", Color = "#10B981" };
        var food = new FakeCategory { Id = "cat-food", Name = "Food", Color = "#EF4444" };
        var rent = new FakeCategory { Id = "cat-rent", Name = "Rent", Color = "#2563EB" };
        org.Categories.AddRange(new[] { salary, food, rent });

        org.Operations.Add(new FakeOperation { Id = "op-salary", CategoryId = salary.Id, CategoryName = salary.Name, Amount = 5000m, Type = "income", Description = "Salary", CreatedAt = DateTime.UtcNow.AddDays(-2) });
        org.Operations.Add(new FakeOperation { Id = "op-groceries", CategoryId = food.Id, CategoryName = food.Name, Amount = 250m, Type = "expense", Description = "Groceries", CreatedAt = DateTime.UtcNow.AddDays(-1) });
        org.Operations.Add(new FakeOperation { Id = "op-rent", CategoryId = rent.Id, CategoryName = rent.Name, Amount = 1200m, Type = "expense", Description = "Rent", CreatedAt = DateTime.UtcNow.AddDays(-3) });

        Orgs.Add(org);
        ActiveOrg[SeedEmail] = org.Id;
    }
}