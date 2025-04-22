using CashFlow.Transaction.Application.DTOs;
using MediatR;

namespace CashFlow.Transaction.Application.Queries.GetTransactionsByMerchant;

public class GetTransactionsByMerchantQuery : IRequest<IEnumerable<TransactionDto>>
{
    public string MerchantId { get; set; }
    public DateTime? Date { get; set; }
}