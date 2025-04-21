namespace CashFlow.Transaction.Application.Events;

public interface ITransactionEventPublisher
{
    Task PublishTransactionCreatedAsync(Domain.Models.Transaction transaction);
}