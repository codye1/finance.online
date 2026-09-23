namespace finance.online.mvc.Models.DTO
{
    public class OrganizationCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ParticipantUserIds { get; set; } = new();
    }
}
