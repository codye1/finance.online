namespace finance.online.api.Models.DTO
{
    public class OperationUpdateRequestDto
    {
        public string? Type { get; set; }

        public decimal? Amount { get; set; }

        public string? CategoryId { get; set; }

        public string? Description { get; set; }

        public DateTime? Date { get; set; }
    }
}