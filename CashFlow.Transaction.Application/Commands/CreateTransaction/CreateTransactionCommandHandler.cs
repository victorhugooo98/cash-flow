// CashFlow.Transaction.Application/Commands/CreateTransaction/CreateTransactionCommandHandler.cs

using CashFlow.Transaction.Application.Events;
using CashFlow.Transaction.Domain.Models;
using CashFlow.Transaction.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CashFlow.Transaction.Application.Commands.CreateTransaction;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionEventPublisher _eventPublisher;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        ITransactionEventPublisher eventPublisher,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transactionType = Enum.Parse<TransactionType>(request.Type);

        // Use the factory method instead of constructor
        var transaction = Domain.Models.Transaction.Create(
            request.MerchantId,
            request.Amount,
            transactionType,
            request.Description);

        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        try
        {
            await _eventPublisher.PublishTransactionCreatedAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event for transaction {TransactionId}. Transaction saved but consolidation may be delayed.",
                transaction.Id);
        }

        return transaction.Id;
    }
}