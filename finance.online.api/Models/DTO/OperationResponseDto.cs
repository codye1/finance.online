namespace finance.online.api.Models.DTO
{
    public class OperationResponseDto
    {
        public string Id { get; set; } = null!;

        public string Type { get; set; } = null!;

        public int Amount { get; set; }

        public string CategoryId { get; set; } = null!;

        public string CategoryName { get; set; } = null!;

        public string CategoryColor { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedById { get; set; } = null!;

        public string? CreatedByEmail { get; set; }

        public string? CreatedByFullName { get; set; }

        public string OrganizationId { get; set; } = null!;
    }
}