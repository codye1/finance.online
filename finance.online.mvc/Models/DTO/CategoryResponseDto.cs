namespace finance.online.mvc.Models.DTO
{
    public class CategoryResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string OrganizationId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}