using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Application.Services;

public class DailyBalanceService : IDailyBalanceService
{
    private readonly IDailyBalanceRepository _balanceRepository;
    private readonly ILogger<DailyBalanceService> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Add this for sync

    public DailyBalanceService(
        IDailyBalanceRepository balanceRepository,
        ILogger<DailyBalanceService> logger)
    {
        _balanceRepository = balanceRepository;
        _logger = logger;
    }

    public async Task ProcessTransactionAsync(TransactionCreatedEvent transactionEvent)
    {
        var transactionDate = transactionEvent.Timestamp.Date;
        var merchantId = transactionEvent.MerchantId;

        _logger.LogInformation(
            "Processing transaction {TransactionId} for merchant {MerchantId}",
            transactionEvent.TransactionId,
            transactionEvent.MerchantId);

        try
        {
            // Synchronize access to the daily balance for this merchant/date
            await _semaphore.WaitAsync();

            _logger.LogInformation(
                "Fetching balance for {MerchantId} on {Date}",
                merchantId, transactionDate);

            // Get or create daily balance for this date
            var dailyBalance = await _balanceRepository.GetByMerchantAndDateAsync(merchantId, transactionDate);

            if (dailyBalance == null)
            {
                // Get previous day's closing balance
                var previousDayBalance =
                    await _balanceRepository.GetPreviousDayBalanceAsync(merchantId, transactionDate);
                var openingBalance = previousDayBalance?.ClosingBalance ?? 0;

                dailyBalance = new DailyBalance(merchantId, transactionDate, openingBalance);
                await _balanceRepository.AddAsync(dailyBalance);

                _logger.LogInformation(
                    "Created new daily balance for merchant {MerchantId} on {Date} with opening balance {OpeningBalance}",
                    merchantId, transactionDate, openingBalance);
            }
            else
            {
                _logger.LogInformation(
                    "Found existing balance for {MerchantId} on {Date}: OpeningBalance={OpeningBalance}, Credits={Credits}, Debits={Debits}, ClosingBalance={ClosingBalance}",
                    merchantId, transactionDate, dailyBalance.OpeningBalance, dailyBalance.TotalCredits, 
                    dailyBalance.TotalDebits, dailyBalance.ClosingBalance);
            }

            _logger.LogInformation("Processing transaction: Type={Type}, Amount={Amount}", 
                transactionEvent.Type, transactionEvent.Amount);

            // Update the daily balance
            if (transactionEvent.Type == "Credit")
            {
                dailyBalance.AddCredit(transactionEvent.Amount);
                _logger.LogInformation(
                    "Added Credit. New Total Credits={Credits}, New Closing Balance={Balance}",
                    dailyBalance.TotalCredits, dailyBalance.ClosingBalance);
            }
            else if (transactionEvent.Type == "Debit")
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

            _logger.LogInformation(
                "Attempting to save daily balance for {MerchantId} on {Date}",
                merchantId, transactionDate);

            await _balanceRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully saved daily balance for {MerchantId} on {Date}.",
                merchantId, transactionDate);

            _logger.LogInformation(
                "Processing transaction {TransactionId} with amount {Amount} and type {Type}",
                transactionEvent.TransactionId, transactionEvent.Amount, transactionEvent.Type);

            _logger.LogInformation(
                "Successfully processed transaction event {TransactionId}",
                transactionEvent.TransactionId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Retry the operation after fetching the latest data
            _logger.LogWarning(ex, 
                "Concurrency conflict when processing transaction {TransactionId}. Retrying operation.",
                transactionEvent.TransactionId);
            
            // You could implement a retry here
            throw;
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException?.Message.Contains("duplicate key") == true)
            {
                // Handle duplicate key - retry fetching and updating
                _logger.LogWarning(ex, 
                    "Duplicate key when processing transaction {TransactionId}. Another thread created the record.",
                    transactionEvent.TransactionId);
                
                // You could implement a retry here
                throw;
            }
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public interface IDailyBalanceService
{
    Task ProcessTransactionAsync(TransactionCreatedEvent transactionEvent);
}