namespace CashFlow.Consolidation.Application.Interfaces;

public interface IProcessedTransactionsIdempotencyService
{
    Task<bool> HasBeenProcessedAsync(Guid transactionId);
    Task MarkAsProcessedAsync(Guid transactionId, DateTime processedAt);
}