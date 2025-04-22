namespace CashFlow.Consolidation.Application.Interfaces;

public interface IProcessedMessagesIdempotencyService
{
    Task<bool> HasBeenProcessedAsync(Guid messageId);
    Task MarkAsProcessedAsync(Guid messageId, DateTime processedAt);
}