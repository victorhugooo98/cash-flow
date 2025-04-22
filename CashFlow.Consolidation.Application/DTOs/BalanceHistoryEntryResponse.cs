namespace CashFlow.Consolidation.Application.DTOs;

public class BalanceHistoryEntryResponse
{
    public string Date { get; set; } = null!;
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal NetChange { get; set; }
}