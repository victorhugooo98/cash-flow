using CashFlow.Shared.Events;
using CashFlow.Transaction.Application.Events;
using MassTransit;

namespace CashFlow.Transaction.Infrastructure.Messaging;

public class TransactionEventPublisher : ITransactionEventPublisher
{
    private readonly IBus _bus;
        
    public TransactionEventPublisher(IBus bus)
    {
        _bus = bus;
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
            
        await _bus.Publish(@event);
    }
}