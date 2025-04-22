using CashFlow.Shared.Exceptions;

namespace CashFlow.Transaction.Domain.Models;

public enum TransactionType
{
    Credit = 0,
    Debit = 1
}

public class Transaction
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public string MerchantId { get; private set; } = string.Empty;

    // Private constructor for EF Core
    public Transaction()
    {
    }

    public static Transaction Create(string merchantId, decimal amount, TransactionType type, string description)
    {
        ValidateInputs(merchantId, amount, description);

        return new Transaction
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = amount,
            Type = type,
            Description = description,
            Timestamp = DateTime.UtcNow
        };
    }

    private static void ValidateInputs(string merchantId, decimal amount, string description)
    {
        var validationResults = new List<string>();

        if (string.IsNullOrWhiteSpace(merchantId))
            validationResults.Add("Merchant ID cannot be empty");

        if (amount <= 0)
            validationResults.Add("Amount must be greater than zero");

        if (string.IsNullOrWhiteSpace(description))
            validationResults.Add("Description cannot be empty");

        if (validationResults.Any())
            throw new TransactionValidationException(validationResults);
    }
}