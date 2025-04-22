using CashFlow.Consolidation.Application.Interfaces;
using CashFlow.Consolidation.Application.Services;
using CashFlow.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Infrastructure.Messaging;

public class TransactionEventConsumer : IConsumer<TransactionCreatedEvent>
{
    private readonly IDailyBalanceService _balanceService;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<TransactionEventConsumer> _logger;

    public TransactionEventConsumer(
        IDailyBalanceService balanceService,
        IIdempotencyService idempotencyService,
        ILogger<TransactionEventConsumer> logger)
    {
        _balanceService = balanceService;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreatedEvent> context)
    {
        var @event = context.Message;
        var messageId = context.MessageId ?? Guid.NewGuid();

        try
        {
            // Check if this message has already been processed
            if (await _idempotencyService.HasBeenProcessedAsync(messageId))
            {
                _logger.LogInformation(
                    "Message {MessageId} for transaction {TransactionId} already processed, skipping",
                    messageId, @event.TransactionId);
                return;
            }

            _logger.LogInformation(
                "Processing message {MessageId} for transaction {TransactionId}",
                messageId, @event.TransactionId);

            // Process the transaction
            await _balanceService.ProcessTransactionAsync(@event);
            
            // Mark as processed to ensure idempotency
            await _idempotencyService.MarkAsProcessedAsync(messageId, DateTime.UtcNow);
            
            _logger.LogInformation(
                "Successfully processed transaction event {TransactionId}",
                @event.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction event {TransactionId}", @event.TransactionId);
            throw; // Re-throw to trigger retry policy
        }
    }
}