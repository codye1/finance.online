
namespace finance.online.api.Models.DTO
{
    public class CashflowResponseDto
    {
        public string Granularity { get; set; } = "day";
        public List<CashflowPointDto> Points { get; set; } = new();
    }

    public class CashflowPointDto
    {
        public DateTime Date { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }
}