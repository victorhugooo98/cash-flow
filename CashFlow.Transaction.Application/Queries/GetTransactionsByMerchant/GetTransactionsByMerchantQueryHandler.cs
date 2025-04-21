using CashFlow.Transaction.Application.DTOs;
using CashFlow.Transaction.Domain.Repositories;
using MediatR;

namespace CashFlow.Transaction.Application.Queries.GetTransactionsByMerchant;

public class GetTransactionsByMerchantQueryHandler : IRequestHandler<GetTransactionsByMerchantQuery, IEnumerable<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepository;
        
    public GetTransactionsByMerchantQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }
        
    public async Task<IEnumerable<TransactionDto>> Handle(GetTransactionsByMerchantQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _transactionRepository.GetByMerchantIdAsync(request.MerchantId, request.Date);
            
        return transactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            MerchantId = t.MerchantId,
            Amount = t.Amount,
            Type = t.Type.ToString(),
            Description = t.Description,
            Timestamp = t.Timestamp
        });
    }
}