using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using RabbitMQ.Client.Exceptions;

namespace CashFlow.Consolidation.Infrastructure.Resilience;

public class CircuitBreakerPolicyProvider
{
    private readonly ILogger<CircuitBreakerPolicyProvider> _logger;
    private readonly IOptions<CircuitBreakerOptions> _options;

    public CircuitBreakerPolicyProvider(
        ILogger<CircuitBreakerPolicyProvider> logger,
        IOptions<CircuitBreakerOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public AsyncCircuitBreakerPolicy GetPolicy<TException>() where TException : Exception
    {
        return Policy
            .Handle<TException>()
            .CircuitBreakerAsync(
                _options.Value.ExceptionsAllowedBeforeBreaking,
                _options.Value.DurationOfBreak,
                OnBreak,
                OnReset,
                OnHalfOpen);
    }

    public AsyncCircuitBreakerPolicy GetDatabasePolicy()
    {
        return Policy
            .Handle<SqlException>()
            .Or<DbUpdateException>()
            .CircuitBreakerAsync(
                _options.Value.DatabaseExceptionsAllowedBeforeBreaking,
                _options.Value.DatabaseDurationOfBreak,
                OnBreak,
                OnReset,
                OnHalfOpen);
    }

    public AsyncCircuitBreakerPolicy GetMessageBrokerPolicy()
    {
        return Policy
            .Handle<TransportException>()
            .Or<ConnectionException>()
            .Or<BrokerUnreachableException>()
            .Or<ConnectFailureException>()
            .CircuitBreakerAsync(
                _options.Value.MessageBrokerExceptionsAllowedBeforeBreaking,
                _options.Value.MessageBrokerDurationOfBreak,
                OnBreak,
                OnReset,
                OnHalfOpen);
    }

    private void OnBreak(Exception ex, TimeSpan duration)
    {
        _logger.LogWarning(ex, "Circuit breaker tripped for {Duration}s", duration.TotalSeconds);
        // Alert monitoring systems
    }

    private void OnReset()
    {
        _logger.LogInformation("Circuit breaker reset");
    }

    private void OnHalfOpen()
    {
        _logger.LogInformation("Circuit breaker half-open");
    }
}

public class CircuitBreakerOptions
{
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
    
    public int DatabaseExceptionsAllowedBeforeBreaking { get; set; } = 3;
    public TimeSpan DatabaseDurationOfBreak { get; set; } = TimeSpan.FromSeconds(15);
    
    public int MessageBrokerExceptionsAllowedBeforeBreaking { get; set; } = 5;
    public TimeSpan MessageBrokerDurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
}