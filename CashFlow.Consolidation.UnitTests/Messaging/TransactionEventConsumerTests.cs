using CashFlow.Consolidation.Application.Interfaces;
using CashFlow.Consolidation.Application.Services;
using CashFlow.Consolidation.Infrastructure.Messaging;
using CashFlow.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace CashFlow.Consolidation.UnitTests.Messaging;

public class TransactionEventConsumerTests
{
    private readonly Mock<IDailyBalanceService> _mockBalanceService;
    private readonly Mock<IProcessedTransactionsIdempotencyService> _mockIdempotencyService; // Make sure this matches
    private readonly Mock<ILogger<TransactionEventConsumer>> _mockLogger;
    private readonly TransactionEventConsumer _consumer;
    private readonly Mock<ConsumeContext<TransactionCreatedEvent>> _mockConsumeContext;

    public TransactionEventConsumerTests()
    {
        _mockBalanceService = new Mock<IDailyBalanceService>();
        _mockIdempotencyService = new Mock<IProcessedTransactionsIdempotencyService>(); // Updated interface name
        _mockLogger = new Mock<ILogger<TransactionEventConsumer>>();
        _consumer = new TransactionEventConsumer(
            _mockBalanceService.Object,
            _mockIdempotencyService.Object,
            _mockLogger.Object);
        _mockConsumeContext = new Mock<ConsumeContext<TransactionCreatedEvent>>();

        // Setup idempotency check to allow tests to proceed
        _mockIdempotencyService
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Give a message ID to the context
        _mockConsumeContext
            .Setup(c => c.MessageId)
            .Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Consume_ExceptionOccurs_ShouldRethrowException()
    {
        // Arrange
        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = "test-merchant",
            Amount = 100.00m,
            Type = "Credit",
            Description = "Exception Test",
            Timestamp = DateTime.UtcNow.Date
        };

        _mockConsumeContext.Setup(c => c.Message).Returns(transactionEvent);
        _mockBalanceService
            .Setup(s => s.ProcessTransactionAsync(It.IsAny<TransactionCreatedEvent>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _consumer.Consume(_mockConsumeContext.Object));

        // Verify idempotency service was checked but message wasn't marked as processed
        _mockIdempotencyService.Verify(s => s.HasBeenProcessedAsync(It.IsAny<Guid>()), Times.Once);
        _mockIdempotencyService.Verify(s => s.MarkAsProcessedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_ConsumerInvoked_LogsInformation()
    {
        // Arrange
        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = "test-merchant",
            Amount = 100.00m,
            Type = "Credit",
            Description = "Logging Test",
            Timestamp = DateTime.UtcNow.Date
        };

        _mockConsumeContext.Setup(c => c.Message).Returns(transactionEvent);

        // Configure idempotency service to mark message as processed
        _mockIdempotencyService
            .Setup(s => s.MarkAsProcessedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(_mockConsumeContext.Object);

        // Assert - Verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.AtLeastOnce);

        // Verify idempotency service was called correctly
        _mockIdempotencyService.Verify(s => s.HasBeenProcessedAsync(It.IsAny<Guid>()), Times.Once);
        _mockIdempotencyService.Verify(s => s.MarkAsProcessedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Consume_AlreadyProcessedMessage_SkipsProcessing()
    {
        // Arrange
        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = "test-merchant",
            Amount = 100.00m,
            Type = "Credit",
            Description = "Idempotency Test",
            Timestamp = DateTime.UtcNow.Date
        };

        _mockConsumeContext.Setup(c => c.Message).Returns(transactionEvent);

        // Configure idempotency service to return true (already processed)
        _mockIdempotencyService
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        await _consumer.Consume(_mockConsumeContext.Object);

        // Assert - Verify balance service was not called
        _mockBalanceService.Verify(
            s => s.ProcessTransactionAsync(It.IsAny<TransactionCreatedEvent>()),
            Times.Never);

        // Verify message was not marked as processed again
        _mockIdempotencyService.Verify(
            s => s.MarkAsProcessedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()),
            Times.Never);
    }
}