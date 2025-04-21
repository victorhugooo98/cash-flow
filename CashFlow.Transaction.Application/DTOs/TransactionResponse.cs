namespace CashFlow.Transaction.Application.DTOs;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string MerchantId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = "Processed";
}