namespace tests.Mocks.Models;

public sealed class FakeCategory
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#2563EB";
}