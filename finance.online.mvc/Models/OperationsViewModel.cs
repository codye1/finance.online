using finance.online.mvc.Models.DTO;

namespace finance.online.mvc.Models
{
    public class OperationsViewModel
    {
        public bool HasOrganizations { get; set; }
        public OrganizationResponseDto? ActiveOrganization { get; set; }
        public List<OperationListItemViewModel> Operations { get; set; } = new();
        public List<OperationCategoryOptionViewModel> Categories { get; set; } = new();
    }

    public class OperationListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class OperationCategoryOptionViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}