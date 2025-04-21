namespace CashFlow.Consolidation.Application.DTOs
{
    public class DailyBalanceDto
    {
        public Guid Id { get; set; }
        public string MerchantId { get; set; }
        public DateTime Date { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}