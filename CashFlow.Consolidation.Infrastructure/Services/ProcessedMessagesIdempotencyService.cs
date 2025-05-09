using CashFlow.Consolidation.Application.Interfaces;
using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Infrastructure.Services;

public class ProcessedMessagesIdempotencyService : IProcessedMessagesIdempotencyService
{
    private readonly ConsolidationDbContext _dbContext;
    private readonly ILogger<ProcessedMessagesIdempotencyService> _logger;

    public ProcessedMessagesIdempotencyService(
        ConsolidationDbContext dbContext,
        ILogger<ProcessedMessagesIdempotencyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> HasBeenProcessedAsync(Guid messageId)
    {
        return await _dbContext.ProcessedMessages
            .AnyAsync(m => m.MessageId == messageId);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, DateTime processedAt)
    {
        var message = new ProcessedMessage
        {
            MessageId = messageId,
            ProcessedAt = processedAt
        };

        await _dbContext.ProcessedMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();
    }
}