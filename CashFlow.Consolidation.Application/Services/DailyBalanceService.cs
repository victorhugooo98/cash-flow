using CashFlow.Consolidation.Application.Interfaces;
using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Application.Services;

public class DailyBalanceService : IDailyBalanceService
{
    private readonly IDailyBalanceRepository _balanceRepository;
    private readonly IDistributedLockManager _lockManager;
    private readonly ILogger<DailyBalanceService> _logger;

    public DailyBalanceService(
        IDailyBalanceRepository balanceRepository,
        IDistributedLockManager lockManager,
        ILogger<DailyBalanceService> logger)
    {
        _balanceRepository = balanceRepository;
        _lockManager = lockManager;
        _logger = logger;
    }

    public async Task ProcessTransactionAsync(TransactionCreatedEvent transactionEvent)
    {
        var transactionDate = transactionEvent.Timestamp.Date;
        var merchantId = transactionEvent.MerchantId;
        var lockKey = $"balance:{merchantId}:{transactionDate:yyyy-MM-dd}";

        _logger.LogInformation(
            "Processing transaction {TransactionId} for merchant {MerchantId}",
            transactionEvent.TransactionId, merchantId);

        // Acquire a distributed lock for this merchant and date combination
        using (var lockHandle = await _lockManager.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10)))
        {
            try
            {
                _logger.LogInformation(
                    "Fetching balance for {MerchantId} on {Date}",
                    merchantId, transactionDate);

                // Get existing balance for this date
                var dailyBalance = await _balanceRepository.GetByMerchantAndDateAsync(merchantId, transactionDate);

                if (dailyBalance == null)
                    // Create new balance record
                    dailyBalance = await CreateNewDailyBalanceAsync(merchantId, transactionDate);
                else
                    _logger.LogInformation(
                        "Found existing balance for {MerchantId} on {Date}: OpeningBalance={OpeningBalance}, " +
                        "Credits={Credits}, Debits={Debits}, ClosingBalance={ClosingBalance}",
                        merchantId, transactionDate, dailyBalance.OpeningBalance, dailyBalance.TotalCredits,
                        dailyBalance.TotalDebits, dailyBalance.ClosingBalance);

                // Update the balance based on transaction type
                if (transactionEvent.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                {
                    dailyBalance.AddCredit(transactionEvent.Amount);
                    _logger.LogInformation(
                        "Added Credit. New Total Credits={Credits}, New Closing Balance={Balance}",
                        dailyBalance.TotalCredits, dailyBalance.ClosingBalance);
                }
                else if (transactionEvent.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                {
                    dailyBalance.AddDebit(transactionEvent.Amount);
                    _logger.LogInformation(
                        "Added Debit. New Total Debits={Debits}, New Closing Balance={Balance}",
                        dailyBalance.TotalDebits, dailyBalance.ClosingBalance);
                }
                else
                {
                    throw new ArgumentException($"Unsupported transaction type: {transactionEvent.Type}",
                        nameof(transactionEvent.Type));
                }

                // Save changes with concurrency handling
                _logger.LogInformation(
                    "Attempting to save daily balance for {MerchantId} on {Date}",
                    merchantId, transactionDate);

                // Attempt to update with concurrency handling
                var updateSuccess = await _balanceRepository.TryUpdateWithConcurrencyHandlingAsync(dailyBalance);

                if (updateSuccess)
                    _logger.LogInformation(
                        "Successfully saved daily balance for {MerchantId} on {Date}.",
                        merchantId, transactionDate);
                else
                    // If update failed after retries, log error but don't throw
                    // This is a recovery mechanism - the message can be retried
                    _logger.LogError(
                        "Failed to update daily balance for {MerchantId} on {Date} after retries.",
                        merchantId, transactionDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing transaction {TransactionId} for merchant {MerchantId} on {Date}",
                    transactionEvent.TransactionId, merchantId, transactionDate);
                throw; // Re-throw for retry via MassTransit
            }
        }
    }

    private async Task<DailyBalance> CreateNewDailyBalanceAsync(string merchantId, DateTime date)
    {
        // Get previous day's closing balance for the opening balance
        var previousDayBalance = await _balanceRepository.GetPreviousDayBalanceAsync(merchantId, date);
        var openingBalance = previousDayBalance?.ClosingBalance ?? 0;

        var newBalance = new DailyBalance(merchantId, date, openingBalance);

        try
        {
            await _balanceRepository.AddAsync(newBalance);
            await _balanceRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Created new daily balance for merchant {MerchantId} on {Date} with opening balance {OpeningBalance}",
                merchantId, date, openingBalance);

            return newBalance;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            // Handle race condition where another thread created the record
            _logger.LogWarning(ex,
                "Another process already created the balance record for {MerchantId} on {Date}. Fetching it.",
                merchantId, date);

            // Get the record that was created by another process
            var existingBalance = await _balanceRepository.GetByMerchantAndDateAsync(merchantId, date);

            if (existingBalance == null)
            {
                _logger.LogError(
                    "Failed to get existing balance after duplicate key error for {MerchantId} on {Date}",
                    merchantId, date);
                throw;
            }

            return existingBalance;
        }
    }
}

public interface IDailyBalanceService
{
    Task ProcessTransactionAsync(TransactionCreatedEvent transactionEvent);
}