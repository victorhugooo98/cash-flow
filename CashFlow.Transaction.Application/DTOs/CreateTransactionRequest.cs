using CashFlow.Transaction.Domain.Models;

namespace CashFlow.Transaction.Application.DTOs;

public record CreateTransactionRequest
{
    public string MerchantId { get; set; } = null!;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = null!;
}