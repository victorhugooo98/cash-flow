using CashFlow.Consolidation.Domain.Models;

namespace CashFlow.Consolidation.Domain.Repositories;

public interface IDailyBalanceRepository
{
    Task<DailyBalance> GetByMerchantAndDateAsync(string merchantId, DateTime date);
    Task<DailyBalance> GetPreviousDayBalanceAsync(string merchantId, DateTime date);
    Task AddAsync(DailyBalance dailyBalance);
    Task UpdateAsync(DailyBalance dailyBalance);
    Task SaveChangesAsync();
}