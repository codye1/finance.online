namespace finance.online.api.Models.DTO
{
    public class CategoryResponseDto
    {
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Color { get; set; } = null!;

        public string OrganizationId { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}