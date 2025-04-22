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

    public async Task TryRecoverStuckRecordsAsync(string merchantId, DateTime date)
    {
        try
        {
            // Get current database locks
            var locks = await _context.GetActiveDatabaseLocksAsync();

            if (locks.Any())
            {
                _logger.LogWarning("Found {LockCount} database locks that might be affecting merchant {MerchantId}",
                    locks.Count, merchantId);

                foreach (var lockInfo in locks) _logger.LogWarning("Active lock: {LockInfo}", lockInfo);
            }

            // Try to re-fetch the entity with tracking disabled
            var entity = await _context.DailyBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MerchantId == merchantId && x.Date == date.Date);

            if (entity != null)
                _logger.LogInformation(
                    "Found record for {MerchantId} on {Date} with Credits={Credits}, Debits={Debits}, Balance={Balance}",
                    merchantId, date.Date, entity.TotalCredits, entity.TotalDebits, entity.ClosingBalance);
            else
                _logger.LogWarning("No record found for {MerchantId} on {Date} during recovery attempt",
                    merchantId, date.Date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attempting to recover stuck records for {MerchantId} on {Date}",
                merchantId, date);
        }
    }

    public async Task<DailyBalance?> GetByMerchantAndDateAsync(string merchantId, DateTime date)
    {
        return await _context.DailyBalances
            .FirstOrDefaultAsync(b => b.MerchantId == merchantId && b.Date == date);
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
        _context.Entry(dailyBalance).State = EntityState.Modified;
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TryUpdateWithConcurrencyHandlingAsync(DailyBalance dailyBalance, int maxRetries = 3)
    {
        var retryCount = 0;
        var success = false;

        while (retryCount < maxRetries && !success)
            try
            {
                if (retryCount > 0)
                {
                    // Re-fetch the entity on retries to get the latest version
                    _context.Entry(dailyBalance).State = EntityState.Detached;
                    var freshEntity = await GetByMerchantAndDateAsync(dailyBalance.MerchantId, dailyBalance.Date);

                    if (freshEntity == null)
                    {
                        _logger.LogWarning("Entity not found during retry for {MerchantId} on {Date}",
                            dailyBalance.MerchantId, dailyBalance.Date);
                        return false;
                    }

                    // Apply the changes to the fresh entity
                    freshEntity.AddCredit(dailyBalance.TotalCredits);
                    freshEntity.AddDebit(dailyBalance.TotalDebits);

                    // Update reference to use fresh entity
                    dailyBalance = freshEntity;
                }

                // Make the update
                await UpdateAsync(dailyBalance);
                await SaveChangesAsync();
                success = true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict detected on attempt {RetryCount} for {MerchantId} on {Date}",
                    retryCount + 1, dailyBalance.MerchantId, dailyBalance.Date);

                retryCount++;

                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex,
                        "Max retries ({MaxRetries}) reached for concurrency conflict for {MerchantId} on {Date}",
                        maxRetries, dailyBalance.MerchantId, dailyBalance.Date);
                    return false;
                }

                // Add a small delay before retrying
                await Task.Delay(100 * retryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating daily balance for {MerchantId} on {Date}",
                    dailyBalance.MerchantId, dailyBalance.Date);
                return false;
            }

        return success;
    }
}