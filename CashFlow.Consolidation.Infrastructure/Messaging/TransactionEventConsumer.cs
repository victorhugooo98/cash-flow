using CashFlow.Consolidation.Application.Services;
using CashFlow.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Infrastructure.Messaging;

public class TransactionEventConsumer : IConsumer<TransactionCreatedEvent>
{
    private readonly IDailyBalanceService _balanceService;
    private readonly ILogger<TransactionEventConsumer> _logger;

    public TransactionEventConsumer(
        IDailyBalanceService balanceService,
        ILogger<TransactionEventConsumer> logger)
    {
        _balanceService = balanceService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreatedEvent> context)
    {
        var @event = context.Message;

        try
        {
            _logger.LogInformation(
                "Received transaction event {TransactionId} for merchant {MerchantId}",
                @event.TransactionId, @event.MerchantId);

            await _balanceService.ProcessTransactionAsync(@event);
            
            _logger.LogInformation(
                "Successfully processed transaction event {TransactionId}",
                @event.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction event {TransactionId}", @event.TransactionId);
            throw; // Re-throw to trigger retry policy
        }
    }
}