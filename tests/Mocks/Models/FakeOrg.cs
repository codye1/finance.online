namespace tests.Mocks.Models;

public sealed class FakeOrg
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<FakeMember> Members { get; } = new();
    public List<FakeCategory> Categories { get; } = new();
    public List<FakeOperation> Operations { get; } = new();
}