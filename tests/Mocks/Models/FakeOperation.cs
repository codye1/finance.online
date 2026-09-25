namespace tests.Mocks.Models;

public sealed class FakeOperation
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string CategoryId { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Type { get; set; } = "expense";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}