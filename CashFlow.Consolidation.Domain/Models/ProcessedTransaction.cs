namespace CashFlow.Consolidation.Domain.Models;

public class ProcessedTransaction
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    // Private constructor for EF Core
    private ProcessedTransaction()
    {
    }

    public ProcessedTransaction(Guid transactionId, DateTime processedAt)
    {
        Id = Guid.NewGuid();
        TransactionId = transactionId;
        ProcessedAt = processedAt;
    }
}