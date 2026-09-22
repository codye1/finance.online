namespace finance.online.api.Models.DTO
{
    public class OperationsSummaryDto
    {
        public string Period { get; set; } = null!;

        public decimal Income { get; set; }

        public decimal Expense { get; set; }

        public decimal NetProfit { get; set; }

        public decimal Margin { get; set; }
    }
}