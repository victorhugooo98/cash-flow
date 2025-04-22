using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Consolidation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Repositories;

public class DailyBalanceRepository(ConsolidationDbContext context) : IDailyBalanceRepository
{
    public async Task<DailyBalance?> GetByMerchantAndDateAsync(string merchantId, DateTime date)
    {
        return await context.DailyBalances
            .FirstOrDefaultAsync(b => b.MerchantId == merchantId && b.Date == date.Date);
    }

    public async Task<DailyBalance?> GetPreviousDayBalanceAsync(string merchantId, DateTime date)
    {
        var previousDay = date.Date.AddDays(-1);

        return await context.DailyBalances
            .Where(b => b.MerchantId == merchantId && b.Date <= previousDay)
            .OrderByDescending(b => b.Date)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(DailyBalance dailyBalance)
    {
        await context.DailyBalances.AddAsync(dailyBalance);
    }

    public async Task UpdateAsync(DailyBalance dailyBalance)
    {
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}