namespace CashFlow.Transaction.Domain.Models;

public enum TransactionType
{
    Credit,
    Debit
}

public class Transaction
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public string Description { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string MerchantId { get; private set; }

    // Private constructor for EF Core
    private Transaction()
    {
    }

    public Transaction(string merchantId, decimal amount, TransactionType type, string description)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant ID cannot be empty", nameof(merchantId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        Id = Guid.NewGuid();
        MerchantId = merchantId;
        Amount = amount;
        Type = type;
        Description = description;
        Timestamp = DateTime.UtcNow;
    }
}