namespace CashFlow.Transaction.Application.DTOs;

public record TransactionResponse
{
    public Guid Id { get; set; }
    public string MerchantId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Type { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = "Processed";
}