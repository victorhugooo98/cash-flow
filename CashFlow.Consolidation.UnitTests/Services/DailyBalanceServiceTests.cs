using CashFlow.Consolidation.Application.Services;
using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Shared.Events;
using CashFlow.Transaction.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace CashFlow.Consolidation.UnitTests.Services;

public class DailyBalanceServiceTests
{
    private readonly Mock<IDailyBalanceRepository> _mockRepository;
    private readonly Mock<ILogger<DailyBalanceService>> _mockLogger;
    private readonly DailyBalanceService _service;

    public DailyBalanceServiceTests()
    {
        _mockRepository = new Mock<IDailyBalanceRepository>();
        _mockLogger = new Mock<ILogger<DailyBalanceService>>();
        _service = new DailyBalanceService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessTransactionAsync_CreditTransaction_ShouldAddCreditToExistingBalance()
    {
        // Arrange
        var merchantId = "test-merchant";
        var transactionDate = DateTime.UtcNow.Date;
        var transactionAmount = 100.50m;

        var existingBalance = new DailyBalance(merchantId, transactionDate, 500.00m);
        var initialClosingBalance = existingBalance.ClosingBalance;

        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = transactionAmount,
            Type = TransactionType.Credit.ToString(),
            Description = "Test Credit Transaction",
            Timestamp = transactionDate
        };

        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, transactionDate))
            .ReturnsAsync(existingBalance);

        // Act
        await _service.ProcessTransactionAsync(transactionEvent);

        // Assert
        Assert.Equal(initialClosingBalance + transactionAmount, existingBalance.ClosingBalance);
        Assert.Equal(transactionAmount, existingBalance.TotalCredits);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionAsync_DebitTransaction_ShouldAddDebitToExistingBalance()
    {
        // Arrange
        var merchantId = "test-merchant";
        var transactionDate = DateTime.UtcNow.Date;
        var transactionAmount = 50.25m;

        var existingBalance = new DailyBalance(merchantId, transactionDate, 500.00m);
        var initialClosingBalance = existingBalance.ClosingBalance;

        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = transactionAmount,
            Type = TransactionType.Debit.ToString(),
            Description = "Test Debit Transaction",
            Timestamp = transactionDate
        };

        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, transactionDate))
            .ReturnsAsync(existingBalance);

        // Act
        await _service.ProcessTransactionAsync(transactionEvent);

        // Assert
        Assert.Equal(initialClosingBalance - transactionAmount, existingBalance.ClosingBalance);
        Assert.Equal(transactionAmount, existingBalance.TotalDebits);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionAsync_FirstTransactionForDay_ShouldCreateNewDailyBalance()
    {
        // Arrange
        var merchantId = "test-merchant";
        var transactionDate = DateTime.UtcNow.Date;
        var transactionAmount = 10;
        var previousDayBalance = new DailyBalance(merchantId, transactionDate.AddDays(-1), 200.00m);
        previousDayBalance.AddCredit(300.00m); // Closing balance is now 500.00m

        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = transactionAmount,
            Type = TransactionType.Credit.ToString(),
            Description = "First Transaction of Day",
            Timestamp = transactionDate
        };

        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, transactionDate))
            .ReturnsAsync((DailyBalance)null);
        _mockRepository
            .Setup(r => r.GetPreviousDayBalanceAsync(merchantId, transactionDate))
            .ReturnsAsync(previousDayBalance);

        DailyBalance capturedBalance = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DailyBalance>()))
            .Callback<DailyBalance>(b => capturedBalance = b);

        // Act
        await _service.ProcessTransactionAsync(transactionEvent);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<DailyBalance>()), Times.Once);
        Assert.NotNull(capturedBalance);
        Assert.Equal(merchantId, capturedBalance.MerchantId);
        Assert.Equal(transactionDate, capturedBalance.Date);
        Assert.Equal(500.00m, capturedBalance.OpeningBalance); // Previous day's closing balance
        Assert.Equal(transactionAmount, capturedBalance.TotalCredits);
        Assert.Equal(500.00m + transactionAmount, capturedBalance.ClosingBalance);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionAsync_InvalidTransactionType_ShouldThrowException()
    {
        // Arrange
        var merchantId = "test-merchant";
        var transactionDate = DateTime.UtcNow.Date;
        var transactionAmount = 100.00m;

        var existingBalance = new DailyBalance(merchantId, transactionDate, 500.00m);

        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = transactionAmount,
            Type = "InvalidType", // Invalid type
            Description = "Transaction with Invalid Type",
            Timestamp = transactionDate
        };

        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, transactionDate))
            .ReturnsAsync(existingBalance);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ProcessTransactionAsync(transactionEvent));

        Assert.Contains("Unsupported transaction type", exception.Message);
    }
}