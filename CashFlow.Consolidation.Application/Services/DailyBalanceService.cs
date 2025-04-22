using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Shared.Events;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Application.Services;

public class DailyBalanceService : IDailyBalanceService
{
    private readonly IDailyBalanceRepository _balanceRepository;
    private readonly ILogger<DailyBalanceService> _logger;

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
            "Processing transaction {TransactionId} for merchant {MerchantId} on {Date}",
            transactionEvent.TransactionId, merchantId, transactionDate);

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

        // Update the daily balance
        if (transactionEvent.Type == "Credit")
        {
            dailyBalance.AddCredit(transactionEvent.Amount);
            _logger.LogInformation(
                "Added credit of {Amount} to merchant {MerchantId} on {Date}",
                transactionEvent.Amount, merchantId, transactionDate);
        }
        else if (transactionEvent.Type == "Debit")
        {
            dailyBalance.AddDebit(transactionEvent.Amount);
            _logger.LogInformation(
                "Added debit of {Amount} to merchant {MerchantId} on {Date}",
                transactionEvent.Amount, merchantId, transactionDate);
        }
        else
        {
            throw new ArgumentException($"Unsupported transaction type: {transactionEvent.Type}", nameof(transactionEvent.Type));
        }

        await _balanceRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Successfully processed transaction {TransactionId} for merchant {MerchantId}. New closing balance: {ClosingBalance}",
            transactionEvent.TransactionId, merchantId, dailyBalance.ClosingBalance);
    }
}

public interface IDailyBalanceService
{
    Task ProcessTransactionAsync(TransactionCreatedEvent transactionEvent);
}