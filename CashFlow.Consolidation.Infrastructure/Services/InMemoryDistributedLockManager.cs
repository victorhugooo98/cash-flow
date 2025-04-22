using CashFlow.Consolidation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CashFlow.Consolidation.Infrastructure.Services;

/// <summary>
/// In-memory implementation of distributed locking.
/// Note: This works only for a single instance. For true distributed locking,
/// use a distributed system like Redis or SQL Server.
/// </summary>
public class InMemoryDistributedLockManager : IDistributedLockManager
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ILogger<InMemoryDistributedLockManager> _logger;

    public InMemoryDistributedLockManager(ILogger<InMemoryDistributedLockManager> logger)
    {
        _logger = logger;
    }

    public async Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan timeout)
    {
        var semaphore = _locks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(1, 1));

        _logger.LogDebug("Attempting to acquire lock for resource {ResourceKey}", resourceKey);

        var lockAcquired = await semaphore.WaitAsync(timeout);

        if (!lockAcquired)
        {
            _logger.LogWarning("Timeout when trying to acquire lock for resource {ResourceKey}", resourceKey);
            throw new TimeoutException($"Timeout acquiring lock for resource {resourceKey}");
        }

        _logger.LogDebug("Lock acquired for resource {ResourceKey}", resourceKey);

        return new LockReleaser(semaphore, resourceKey, _logger);
    }

    private class LockReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly string _resourceKey;
        private readonly ILogger _logger;
        private bool _disposed;

        public LockReleaser(SemaphoreSlim semaphore, string resourceKey, ILogger logger)
        {
            _semaphore = semaphore;
            _resourceKey = resourceKey;
            _logger = logger;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _semaphore.Release();
            _logger.LogDebug("Lock released for resource {ResourceKey}", _resourceKey);
            _disposed = true;
        }
    }
}