using MediatR;
using CashFlow.Transaction.Application.DTOs;

namespace CashFlow.Transaction.Application.Queries.GetTransactionsByMerchant;

public class GetTransactionsByMerchantQuery : IRequest<IEnumerable<TransactionDto>>
{
    public string MerchantId { get; set; }
    public DateTime? Date { get; set; }
}