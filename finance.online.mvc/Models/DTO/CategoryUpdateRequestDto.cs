namespace finance.online.mvc.Models.DTO
{
    public class CategoryUpdateRequestDto
    {
        public string CategoryId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
    }
}