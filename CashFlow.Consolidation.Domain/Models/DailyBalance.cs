namespace CashFlow.Consolidation.Domain.Models;

public class DailyBalance
{
    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public string MerchantId { get; private set; }

    // Add concurrency token for optimistic concurrency
    public byte[] RowVersion { get; private set; }

    // Private constructor for EF Core
    private DailyBalance()
    {
    }

    public DailyBalance(string merchantId, DateTime date, decimal openingBalance)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant ID cannot be empty", nameof(merchantId));

        Id = Guid.NewGuid();
        MerchantId = merchantId;
        Date = date.Date; // Normalize to midnight
        OpeningBalance = openingBalance;
        TotalCredits = 0;
        TotalDebits = 0;
        ClosingBalance = openingBalance;
    }

    public void AddCredit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit amount must be greater than zero", nameof(amount));

        TotalCredits += amount;
        ClosingBalance += amount;
    }

    public void AddDebit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Debit amount must be greater than zero", nameof(amount));

        TotalDebits += amount;
        ClosingBalance -= amount;
    }
}