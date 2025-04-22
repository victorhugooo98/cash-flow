using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace CashFlow.Shared.Resilience;

public class DatabasePolicyProvider
{
    private readonly ILogger<DatabasePolicyProvider> _logger;
    private readonly IOptions<DatabaseRetryOptions> _options;

    public DatabasePolicyProvider(
        ILogger<DatabasePolicyProvider> logger,
        IOptions<DatabaseRetryOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public AsyncRetryPolicy GetRetryPolicy()
    {
        return Policy
            .Handle<SqlException>(ex => IsTransientError(ex))
            .Or<DbUpdateException>(ex => IsTransientError(ex.InnerException as SqlException))
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                _options.Value.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Database operation failed. Retrying {RetryCount}/{TotalRetries} after {RetryTimeSpan}s",
                        retryCount,
                        _options.Value.RetryCount,
                        timeSpan.TotalSeconds);
                });
    }

    private bool IsTransientError(SqlException ex)
    {
        if (ex == null)
            return false;

        // SQL Server transient error codes
        int[] transientErrorNumbers = { 4060, 40197, 40501, 40613, 49918, 49919, 49920, 11001 };
        return transientErrorNumbers.Contains(ex.Number);
    }
}