using CashFlow.Shared.Events;
using CashFlow.Transaction.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CashFlow.Transaction.Infrastructure.Messaging;

public class TransactionEventPublisher : ITransactionEventPublisher
{
    private readonly IBus _bus;
    private readonly ILogger<TransactionEventPublisher> _logger;

    public TransactionEventPublisher(IBus bus, ILogger<TransactionEventPublisher> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task PublishTransactionCreatedAsync(Domain.Models.Transaction transaction)
    {
        var @event = new TransactionCreatedEvent
        {
            TransactionId = transaction.Id,
            MerchantId = transaction.MerchantId,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Description = transaction.Description,
            Timestamp = transaction.Timestamp
        };

        _logger.LogInformation(
            "Publishing transaction created event for transaction {TransactionId}, merchant {MerchantId}, amount {Amount}",
            transaction.Id, transaction.MerchantId, transaction.Amount);

        try
        {
            await _bus.Publish(@event);
            _logger.LogInformation("Successfully published transaction event {TransactionId}", transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish transaction event {TransactionId}", transaction.Id);
            throw; // Re-throw to allow the calling code to handle the exception
        }
    }
}