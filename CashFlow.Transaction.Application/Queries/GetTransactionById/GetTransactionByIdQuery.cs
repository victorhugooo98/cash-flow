using CashFlow.Transaction.Application.DTOs;
using MediatR;

namespace CashFlow.Transaction.Application.Queries.GetTransactionById;

public class GetTransactionByIdQuery : IRequest<TransactionDto>
{
    public Guid Id { get; set; }
}