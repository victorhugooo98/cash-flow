namespace CashFlow.Consolidation.Application.DTOs;

public record DailyBalanceDto
{
    public Guid Id { get; set; }
    public string MerchantId { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal ClosingBalance { get; set; }
}