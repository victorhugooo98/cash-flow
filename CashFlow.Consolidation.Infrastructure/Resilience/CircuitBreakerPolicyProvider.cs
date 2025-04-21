using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace CashFlow.Consolidation.Infrastructure.Resilience;

public class CircuitBreakerPolicyProvider
{
    private readonly ILogger<CircuitBreakerPolicyProvider> _logger;

    public CircuitBreakerPolicyProvider(ILogger<CircuitBreakerPolicyProvider> logger)
    {
        _logger = logger;
    }

    public AsyncCircuitBreakerPolicy GetDefaultPolicy()
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                (ex, breakDuration) =>
                {
                    _logger.LogWarning(ex, "Circuit breaker tripped for {Duration}s", breakDuration.TotalSeconds);
                },
                () => { _logger.LogInformation("Circuit breaker reset"); },
                () => { _logger.LogInformation("Circuit breaker half-open"); });
    }
}