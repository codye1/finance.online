namespace finance.online.api.Models;

public class OperationModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Type { get; set; } = null!;

    public int Amount { get; set; }

    public string CategoryId { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedById { get; set; } = null!;

    public string OrganizationId { get; set; } = null!;

    // Navigation properties
    public Category Category { get; set; } = null!;

    public AppUser CreatedBy { get; set; } = null!;

    public Organization Organization { get; set; } = null!;
}