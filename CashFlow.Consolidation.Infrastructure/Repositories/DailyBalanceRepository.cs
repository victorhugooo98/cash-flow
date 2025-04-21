using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Consolidation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Repositories;

public class DailyBalanceRepository : IDailyBalanceRepository
{
    private readonly ConsolidationDbContext _context;
        
    public DailyBalanceRepository(ConsolidationDbContext context)
    {
        _context = context;
    }
        
    public async Task<DailyBalance> GetByMerchantAndDateAsync(string merchantId, DateTime date)
    {
        return await _context.DailyBalances
            .FirstOrDefaultAsync(b => b.MerchantId == merchantId && b.Date == date.Date);
    }
        
    public async Task<DailyBalance> GetPreviousDayBalanceAsync(string merchantId, DateTime date)
    {
        var previousDay = date.Date.AddDays(-1);
            
        return await _context.DailyBalances
            .Where(b => b.MerchantId == merchantId && b.Date <= previousDay)
            .OrderByDescending(b => b.Date)
            .FirstOrDefaultAsync();
    }
        
    public async Task AddAsync(DailyBalance dailyBalance)
    {
        await _context.DailyBalances.AddAsync(dailyBalance);
    }
        
    public Task UpdateAsync(DailyBalance dailyBalance)
    {
        _context.DailyBalances.Update(dailyBalance);
        return Task.CompletedTask;
    }
        
    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}