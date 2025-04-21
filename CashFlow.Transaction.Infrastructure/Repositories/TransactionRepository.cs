using CashFlow.Transaction.Domain.Repositories;
using CashFlow.Transaction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transaction.Infrastructure.Repositories;

public class TransactionRepository(TransactionDbContext context) : ITransactionRepository
{
    public Task<Domain.Models.Transaction?> GetByIdAsync(Guid id) =>
        context.Transactions.FindAsync(id).AsTask();

    public Task<List<Domain.Models.Transaction>> GetByMerchantIdAsync(string merchantId, DateTime? date = null)
    {
        var query = context.Transactions
            .Where(t => t.MerchantId == merchantId);

        if (!date.HasValue) return query.ToListAsync();
        
        var startDate = date.Value.Date;
        var endDate = startDate.AddDays(1);
        query = query.Where(t => t.Timestamp >= startDate && t.Timestamp < endDate);

        return query.ToListAsync();
    }

    public async Task AddAsync(Domain.Models.Transaction transaction) =>
        await context.Transactions.AddAsync(transaction);

    public Task SaveChangesAsync() =>
        context.SaveChangesAsync();
}