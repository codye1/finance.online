using finance.online.mvc.Models.DTO;

namespace finance.online.mvc.Models
{
    public class HomeViewModel
    {
        public decimal NetBalance { get; set; }

        public OrganizationResponseDto ActiveOrganization { get; set; } = new();
        public List<OrganizationResponseDto> Organizations { get; set; } = new();
        public string ActivePeriod { get; set; } = string.Empty;
        public List<PeriodOption> Periods { get; set; } = new();
        public List<KpiViewModel> Kpis { get; set; } = new();
        public List<OperationViewModel> Operations { get; set; } = new();
        public List<CategoryViewModel> Categories { get; set; } = new();
    }

    public class PeriodOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class KpiViewModel
    {
        public string Tone { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Sub { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class OperationViewModel
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CategoryViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class HomeUserModel
    {
        public string Id { get; set; } = string.Empty;
    }
}