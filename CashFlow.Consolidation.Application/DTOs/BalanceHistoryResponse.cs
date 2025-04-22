namespace CashFlow.Consolidation.Application.DTOs;

public class BalanceHistoryResponse
{
    public string MerchantId { get; set; } = null!;
    public string Period { get; set; } = null!;
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal NetChange { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal FinalBalance { get; set; }
    public int DaysWithActivity { get; set; }
    public decimal AverageDailyCredits { get; set; }
    public decimal AverageDailyDebits { get; set; }
    public decimal DailyBalanceTrend { get; set; }
    public List<BalanceHistoryEntryResponse> Entries { get; set; } = new();

    public static BalanceHistoryResponse FromDto(BalanceHistoryDto dto)
    {
        return new BalanceHistoryResponse
        {
            MerchantId = dto.MerchantId,
            Period = $"{dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}",
            TotalCredits = dto.TotalCredits,
            TotalDebits = dto.TotalDebits,
            NetChange = dto.NetChange,
            InitialBalance = dto.InitialBalance,
            FinalBalance = dto.FinalBalance,
            DaysWithActivity = dto.DaysWithActivity,
            AverageDailyCredits = dto.AverageDailyCredits,
            AverageDailyDebits = dto.AverageDailyDebits,
            DailyBalanceTrend = dto.BalanceTrend,
            Entries = dto.Entries.Select(e => new BalanceHistoryEntryResponse
            {
                Date = e.Date.ToString("yyyy-MM-dd"),
                OpeningBalance = e.OpeningBalance,
                ClosingBalance = e.ClosingBalance,
                TotalCredits = e.TotalCredits,
                TotalDebits = e.TotalDebits,
                NetChange = e.NetChange
            }).ToList()
        };
    }
}