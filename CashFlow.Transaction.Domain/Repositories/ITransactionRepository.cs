namespace CashFlow.Transaction.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Models.Transaction?> GetByIdAsync(Guid id);
    Task<List<Models.Transaction>> GetByMerchantIdAsync(string merchantId, DateTime? date = null);
    Task<List<Models.Transaction>> GetByDateRangeAsync(string merchantId, DateTime startDate, DateTime endDate);
    Task AddAsync(Models.Transaction transaction);
    Task SaveChangesAsync();
}