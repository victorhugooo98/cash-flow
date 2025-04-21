using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace CashFlow.Transaction.API.HealthChecks;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public RabbitMQHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a connection to RabbitMQ
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_connectionString),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // If we got here, we could connect to RabbitMQ
            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection is healthy"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Cannot connect to RabbitMQ", ex));
        }
    }
}