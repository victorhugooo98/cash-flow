using CashFlow.Consolidation.Application.Interfaces;
using CashFlow.Consolidation.Application.Services;
using CashFlow.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Infrastructure.Messaging;

public class TransactionEventConsumer : IConsumer<TransactionCreatedEvent>
{
    private readonly IDailyBalanceService _balanceService;
    private readonly IProcessedTransactionsIdempotencyService _idempotencyService;
    private readonly ILogger<TransactionEventConsumer> _logger;

    public TransactionEventConsumer(
        IDailyBalanceService balanceService,
        IProcessedTransactionsIdempotencyService idempotencyService,
        ILogger<TransactionEventConsumer> logger)
    {
        _balanceService = balanceService;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreatedEvent> context)
    {
        var messageId = context.MessageId ?? Guid.NewGuid();
        var transactionEvent = context.Message;

        _logger.LogInformation(
            "Processing message {MessageId} for transaction {TransactionId}",
            messageId, transactionEvent.TransactionId);

        try
        {
            // Check if this message has already been processed (idempotency)
            if (await _idempotencyService.HasBeenProcessedAsync(transactionEvent.TransactionId))
            {
                _logger.LogInformation(
                    "Transaction {TransactionId} has already been processed, skipping",
                    transactionEvent.TransactionId);
                return;
            }

            // Process the transaction
            await _balanceService.ProcessTransactionAsync(transactionEvent);

            // Mark the transaction as processed
            await _idempotencyService.MarkAsProcessedAsync(transactionEvent.TransactionId, DateTime.UtcNow);

            _logger.LogInformation(
                "Successfully processed transaction event {TransactionId}",
                transactionEvent.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing transaction event {TransactionId}",
                transactionEvent.TransactionId);

            // Rethrow the exception to trigger MassTransit's retry policy
            throw;
        }
    }
}