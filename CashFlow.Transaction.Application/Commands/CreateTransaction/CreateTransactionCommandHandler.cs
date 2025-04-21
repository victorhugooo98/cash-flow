using CashFlow.Transaction.Application.Events;
using CashFlow.Transaction.Domain.Models;
using CashFlow.Transaction.Domain.Repositories;
using MediatR;

namespace CashFlow.Transaction.Application.Commands.CreateTransaction;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionEventPublisher _eventPublisher;

    public CreateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        ITransactionEventPublisher eventPublisher)
    {
        _transactionRepository = transactionRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transactionType = Enum.Parse<TransactionType>(request.Type);

        var transaction = new Domain.Models.Transaction(
            request.MerchantId,
            request.Amount,
            transactionType,
            request.Description);

        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        // Publish event for the consolidation service
        await _eventPublisher.PublishTransactionCreatedAsync(transaction);

        return transaction.Id;
    }
}