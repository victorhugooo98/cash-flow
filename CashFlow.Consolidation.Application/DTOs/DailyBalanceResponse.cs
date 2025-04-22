namespace CashFlow.Consolidation.Application.DTOs;

public record DailyBalanceResponse
{
    public Guid Id { get; init; }
    public string MerchantId { get; init; } = null!;
    public string Date { get; init; } = null!;
    public decimal OpeningBalance { get; init; }
    public decimal TotalCredits { get; init; }
    public decimal TotalDebits { get; init; }
    public decimal ClosingBalance { get; init; }

    public static DailyBalanceResponse FromDto(DailyBalanceDto dto)
    {
        return new DailyBalanceResponse
        {
            Id = dto.Id,
            MerchantId = dto.MerchantId,
            Date = dto.Date.ToString("yyyy-MM-dd"),
            OpeningBalance = dto.OpeningBalance,
            TotalCredits = dto.TotalCredits,
            TotalDebits = dto.TotalDebits,
            ClosingBalance = dto.ClosingBalance
        };
    }
}