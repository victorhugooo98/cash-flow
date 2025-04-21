using CashFlow.Transaction.Domain.Models;

namespace CashFlow.Transaction.Application.DTOs;

public class CreateTransactionRequest
{
    public string MerchantId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; }
}