using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Infrastructure.Messaging;

public class TransactionEventConsumer : IConsumer<TransactionCreatedEvent>
{
    private readonly IDailyBalanceRepository _balanceRepository;
    private readonly ILogger<TransactionEventConsumer> _logger;

    public TransactionEventConsumer(
        IDailyBalanceRepository balanceRepository,
        ILogger<TransactionEventConsumer> logger)
    {
        _balanceRepository = balanceRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreatedEvent> context)
    {
        var @event = context.Message;

        try
        {
            var transactionDate = @event.Timestamp.Date;
            var merchantId = @event.MerchantId;

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
            }

            // Update the daily balance
            if (@event.Type == "Credit")
                dailyBalance.AddCredit(@event.Amount);
            else if (@event.Type == "Debit") dailyBalance.AddDebit(@event.Amount);

            await _balanceRepository.UpdateAsync(dailyBalance);
            await _balanceRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction event {TransactionId}", @event.TransactionId);
            throw; // Re-throw to trigger retry policy
        }
    }
}