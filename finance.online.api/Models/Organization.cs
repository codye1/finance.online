using Azure;

namespace finance.online.api.Models;

public class Organization
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string CreatedById { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public AppUser CreatedBy { get; set; } = null!;

    public ICollection<Member> Members { get; set; } = new List<Member>();

    public ICollection<OperationModel> Operations { get; set; } = new List<OperationModel>();

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}