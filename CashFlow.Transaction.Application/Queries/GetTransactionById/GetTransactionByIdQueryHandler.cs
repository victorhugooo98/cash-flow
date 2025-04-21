using CashFlow.Transaction.Application.DTOs;
using CashFlow.Transaction.Domain.Repositories;
using MediatR;

namespace CashFlow.Transaction.Application.Queries.GetTransactionById;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionByIdQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(request.Id);

        if (transaction == null)
            return null;

        return new TransactionDto
        {
            Id = transaction.Id,
            MerchantId = transaction.MerchantId,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Description = transaction.Description,
            Timestamp = transaction.Timestamp
        };
    }
}