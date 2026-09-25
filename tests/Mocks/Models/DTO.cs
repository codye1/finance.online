
namespace tests.Mocks.Models;

/// <summary>
/// DTO shapes inferred from views / Map* methods in the MVC controllers, plus the
/// small domain helpers (ownership check, period filter) shared across controllers.
/// </summary>
public static class Dto
{
    public static object OrgDto(FakeOrg o, string me) =>
        new { id = o.Id, name = o.Name, description = o.Description, isOwner = IsOwner(o, me) };

    public static object MemberDto(FakeMember m, string me) => new
    {
        email = m.Email,
        role = m.Role,
        isOwner = m.Role == "owner",
        isMe = m.Email == me,
        createdAt = m.CreatedAt
    };

    public static object CategoryDto(FakeCategory c) => new { id = c.Id, name = c.Name, color = c.Color };

    public static object OperationDto(FakeOperation o, FakeOrg org) => new
    {
        id = o.Id,
        organizationId = org.Id,
        categoryId = o.CategoryId,
        categoryName = o.CategoryName,
        amount = o.Amount,
        type = o.Type,
        description = o.Description,
        createdAt = o.CreatedAt
    };

    public static bool IsOwner(FakeOrg org, string me) =>
        org.Members.Any(m => m.Email == me && m.Role == "owner");

    public static IEnumerable<FakeOperation> FilterByPeriod(IEnumerable<FakeOperation> ops, string? period)
    {
        var from = period?.ToLowerInvariant() switch
        {
            "week" => DateTime.UtcNow.AddDays(-7),
            "month" => DateTime.UtcNow.AddDays(-30),
            "year" => DateTime.UtcNow.AddDays(-365),
            _ => DateTime.MinValue
        };
        return ops.Where(o => o.CreatedAt >= from);
    }
}