namespace finance.online.mvc.Models.DTO
{
    public class OperationResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string? CreatedByEmail { get; set; }
        public string? CreatedByFullName { get; set; }
        public string OrganizationId { get; set; } = string.Empty;
    }
}