namespace CashFlow.Shared.Events;

public class TransactionCreatedEvent
{
    public Guid TransactionId { get; set; }
    public string MerchantId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
}