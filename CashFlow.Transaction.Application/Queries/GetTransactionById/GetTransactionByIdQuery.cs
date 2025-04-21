using MediatR;
using CashFlow.Transaction.Application.DTOs;

namespace CashFlow.Transaction.Application.Queries.GetTransactionById;

public class GetTransactionByIdQuery : IRequest<TransactionDto>
{
    public Guid Id { get; set; }
}