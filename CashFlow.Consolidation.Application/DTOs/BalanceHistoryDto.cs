namespace CashFlow.Consolidation.Application.DTOs;

public class BalanceHistoryDto
{
    public string MerchantId { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<BalanceHistoryEntryDto> Entries { get; set; } = new();
    
    // Overall statistics
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal NetChange { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal FinalBalance { get; set; }
    public int DaysWithActivity { get; set; }
    
    // Average daily volumes
    public decimal AverageDailyCredits { get; set; }
    public decimal AverageDailyDebits { get; set; }
    
    // Trends
    public decimal BalanceTrend { get; set; } // Average daily change in balance
}