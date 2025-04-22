// CashFlow.Transaction.Infrastructure/Messaging/TransactionEventPublisher.cs

using CashFlow.Shared.Events;
using CashFlow.Transaction.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System.Threading.Tasks;

namespace CashFlow.Transaction.Infrastructure.Messaging;

public class TransactionEventPublisher : ITransactionEventPublisher
{
    private readonly IBus _bus;
    private readonly ILogger<TransactionEventPublisher> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public TransactionEventPublisher(IBus bus, ILogger<TransactionEventPublisher> logger)
    {
        _bus = bus;
        _logger = logger;
        _circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromMinutes(1),
                (ex, breakDuration) =>
                {
                    _logger.LogWarning(ex,
                        "Circuit breaker tripped. Message broker communication suspended for {BreakDuration}s.",
                        breakDuration.TotalSeconds);
                },
                () => _logger.LogInformation("Circuit breaker reset. Message broker communication resumed."),
                () => _logger.LogInformation("Circuit breaker half-open. Testing message broker communication.")
            );
    }

    public async Task PublishTransactionCreatedAsync(Domain.Models.Transaction transaction)
    {
        var @event = new TransactionCreatedEvent
        {
            TransactionId = transaction.Id,
            MerchantId = transaction.MerchantId,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Description = transaction.Description,
            Timestamp = transaction.Timestamp
        };

        _logger.LogInformation(
            "Publishing transaction created event for transaction {TransactionId}, merchant {MerchantId}, amount {Amount}",
            transaction.Id, transaction.MerchantId, transaction.Amount);

        try
        {
            // Use circuit breaker to isolate from message broker failures
            await _circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                await _bus.Publish(@event);
                _logger.LogInformation("Successfully published transaction event {TransactionId}", transaction.Id);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit open - skipping event publishing for transaction {TransactionId}",
                transaction.Id);
            // Allow operation to continue even if publishing fails
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish transaction event {TransactionId}", transaction.Id);
            // Don't rethrow to prevent transaction service outage if messaging fails
        }
    }
}