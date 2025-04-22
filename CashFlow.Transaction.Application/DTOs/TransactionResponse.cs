namespace CashFlow.Transaction.Application.DTOs;

public record TransactionResponse
{
    public Guid Id { get; init; }
    public string MerchantId { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Type { get; init; } = null!;
    public string Description { get; init; } = null!;
    public DateTime Timestamp { get; init; }
    public string Status { get; init; } = "Processed";
}