namespace CashFlow.Consolidation.Application.Interfaces;

public interface IIdempotencyService
{
    Task<bool> HasBeenProcessedAsync(Guid messageId);
    Task MarkAsProcessedAsync(Guid messageId, DateTime processedAt);
}