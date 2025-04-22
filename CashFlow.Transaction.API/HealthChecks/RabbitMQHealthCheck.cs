using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CashFlow.Transaction.API.HealthChecks;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly string _username;
    private readonly string _password;

    public RabbitMQHealthCheck(string host, string username, string password)
    {
        _host = host;
        _username = username;
        _password = password;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a connection factory
            var factory = new ConnectionFactory
            {
                HostName = _host,
                UserName = _username,
                Password = _password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            };

            // Try to create a connection asynchronously
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            // If we got here, we could connect to RabbitMQ
            return HealthCheckResult.Healthy("RabbitMQ connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cannot connect to RabbitMQ", ex);
        }
    }
}