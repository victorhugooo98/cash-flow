using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Consolidation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Infrastructure.Repositories;

public class DailyBalanceRepository : IDailyBalanceRepository
{
    private readonly ConsolidationDbContext _context;
    private readonly ILogger<DailyBalanceRepository> _logger;

    public DailyBalanceRepository(ConsolidationDbContext context, ILogger<DailyBalanceRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<DailyBalance?> GetByMerchantAndDateAsync(string merchantId, DateTime date)
    {
        return await _context.DailyBalances
            .FirstOrDefaultAsync(b => b.MerchantId == merchantId && b.Date == date.Date);
    }
    
    public async Task UpdateWithConcurrencyHandlingAsync(Guid id, Func<DailyBalance, Task> updateAction)
    {
        const int maxRetries = 3;
        var retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            try
            {
                var balance = await _context.DailyBalances.FindAsync(id);
                if (balance == null)
                {
                    throw new KeyNotFoundException($"Daily balance with ID {id} not found");
                }
                
                await updateAction(balance);
                await _context.SaveChangesAsync();
                
                return; // Success
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict on balance {BalanceId}, retry {Retry}", id, retryCount);
                retryCount++;
                
                if (retryCount >= maxRetries)
                    throw;
                
                // Reset context state for retry
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }
                
                await Task.Delay(50 * retryCount); // Progressive backoff
            }

            retryCount++;
        }
    }

    public async Task<DailyBalance?> GetPreviousDayBalanceAsync(string merchantId, DateTime date)
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

    public async Task UpdateAsync(DailyBalance dailyBalance)
    {
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}