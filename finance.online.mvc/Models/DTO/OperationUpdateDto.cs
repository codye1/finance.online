namespace finance.online.mvc.Models.DTO
{
    public class OperationUpdateDto
    {
        public string? Type { get; set; }
        public decimal? Amount { get; set; }
        public string? CategoryId { get; set; }
        public string? Description { get; set; }
    }
}