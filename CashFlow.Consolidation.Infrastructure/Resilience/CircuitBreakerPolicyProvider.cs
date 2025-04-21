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
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, breakDuration) =>
                {
                    _logger.LogWarning(ex, "Circuit breaker tripped for {Duration}s", breakDuration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open");
                });
    }
}