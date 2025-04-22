using CashFlow.Transaction.Domain.Repositories;
using CashFlow.Transaction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.Transaction.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _context;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(TransactionDbContext context, ILogger<TransactionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Domain.Models.Transaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions.FindAsync(id);
    }

    public async Task<List<Domain.Models.Transaction>> GetByMerchantIdAsync(string merchantId, DateTime? date = null)
    {
        var query = _context.Transactions
            .Where(t => t.MerchantId == merchantId);

        if (date.HasValue)
        {
            var startDate = date.Value.Date;
            var endDate = startDate.AddDays(1);
            query = query.Where(t => t.Timestamp >= startDate && t.Timestamp < endDate);
        }

        return await query.ToListAsync();
    }

    public async Task<List<Domain.Models.Transaction>> GetByDateRangeAsync(string merchantId, DateTime startDate,
        DateTime endDate)
    {
        return await _context.Transactions
            .Where(t => t.MerchantId == merchantId &&
                        t.Timestamp >= startDate.Date &&
                        t.Timestamp < endDate.Date.AddDays(1))
            .OrderBy(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task AddAsync(Domain.Models.Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
        _logger.LogInformation("Added transaction {TransactionId} to context", transaction.Id);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved changes to database");
    }
}