using CashFlow.Consolidation.Application.Interfaces;
using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Infrastructure.Services;

public class ProcessedTransactionsIdempotencyService : IProcessedTransactionsIdempotencyService
{
    private readonly ConsolidationDbContext _dbContext;
    private readonly ILogger<ProcessedTransactionsIdempotencyService> _logger;

    public ProcessedTransactionsIdempotencyService(
        ConsolidationDbContext dbContext,
        ILogger<ProcessedTransactionsIdempotencyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> HasBeenProcessedAsync(Guid transactionId)
    {
        return await _dbContext.ProcessedTransactions
            .AnyAsync(m => m.TransactionId == transactionId);
    }

    public async Task MarkAsProcessedAsync(Guid transactionId, DateTime processedAt)
    {
        var transaction = new ProcessedTransaction(transactionId, processedAt);

        await _dbContext.ProcessedTransactions.AddAsync(transaction);
        await _dbContext.SaveChangesAsync();
    }
}