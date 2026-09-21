using Azure;

namespace finance.online.api.Models;

public class Category
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = null!;

    public string Color { get; set; } = null!;

    public string OrganizationId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;

    public ICollection<OperationModel> Operations { get; set; } = new List<OperationModel>();
}