using CashFlow.Consolidation.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CashFlow.Consolidation.Infrastructure.Services;

public class SqlServerDistributedLockManager : IDistributedLockManager
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerDistributedLockManager> _logger;

    public SqlServerDistributedLockManager(string connectionString, ILogger<SqlServerDistributedLockManager> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan timeout)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        _logger.LogDebug("Attempting to acquire lock for resource {ResourceKey}", resourceKey);

        var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "sp_getapplock";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@Resource", resourceKey);
        command.Parameters.AddWithValue("@LockMode", "Exclusive");
        command.Parameters.AddWithValue("@LockOwner", "Transaction");
        command.Parameters.AddWithValue("@LockTimeout", (int)timeout.TotalMilliseconds);

        var resultParam = command.Parameters.Add("@Result", SqlDbType.Int);
        resultParam.Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync();

        var result = (int)resultParam.Value;
        if (result < 0)
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
            _logger.LogWarning("Failed to acquire SQL lock for resource {ResourceKey}, error: {Error}", resourceKey,
                result);
            throw new TimeoutException($"Failed to acquire SQL lock for resource {resourceKey}");
        }

        _logger.LogDebug("SQL lock acquired for resource {ResourceKey}", resourceKey);

        return new SqlLockReleaser(connection, transaction, resourceKey, _logger);
    }

    private class SqlLockReleaser : IDisposable
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;
        private readonly string _resourceKey;
        private readonly ILogger _logger;
        private bool _disposed;

        public SqlLockReleaser(SqlConnection connection, SqlTransaction transaction, string resourceKey, ILogger logger)
        {
            _connection = connection;
            _transaction = transaction;
            _resourceKey = resourceKey;
            _logger = logger;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _transaction.Dispose();
            _connection.Dispose();
            _logger.LogDebug("SQL lock released for resource {ResourceKey}", _resourceKey);
            _disposed = true;
        }
    }
}