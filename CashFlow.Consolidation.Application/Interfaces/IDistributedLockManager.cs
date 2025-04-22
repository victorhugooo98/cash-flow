namespace CashFlow.Consolidation.Application.Interfaces;

public interface IDistributedLockManager
{
    Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan timeout);
}