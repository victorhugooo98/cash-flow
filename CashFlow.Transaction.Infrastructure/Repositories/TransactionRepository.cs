using CashFlow.Transaction.Domain.Repositories;
using CashFlow.Transaction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transaction.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _context;

    public TransactionRepository(TransactionDbContext context)
    {
        _context = context;
    }

    public Task<Domain.Models.Transaction> GetByIdAsync(Guid id)
    {
        return _context.Transactions.FindAsync(id).AsTask();
    }

    public async Task<IEnumerable<Domain.Models.Transaction>> GetByMerchantIdAsync(string merchantId,
        DateTime? date = null)
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

    public async Task AddAsync(Domain.Models.Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}