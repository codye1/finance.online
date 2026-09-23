namespace finance.online.mvc.Models.DTO
{
    public class OperationCreateDto
    {
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}