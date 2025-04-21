using CashFlow.Transaction.Application.DTOs;
using MediatR;

namespace CashFlow.Transaction.Application.Commands.CreateTransaction;

public class CreateTransactionCommand : IRequest<Guid>
{
    public string MerchantId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
        
    public static CreateTransactionCommand FromRequest(CreateTransactionRequest request)
    {
        return new CreateTransactionCommand
        {
            MerchantId = request.MerchantId,
            Amount = request.Amount,
            Type = request.Type.ToString(),
            Description = request.Description
        };
    }
}