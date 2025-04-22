using CashFlow.Consolidation.Application.Interfaces;
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
    private readonly Mock<IDistributedLockManager> _mockLockManager;
    private readonly Mock<ILogger<DailyBalanceService>> _mockLogger;
    private readonly DailyBalanceService _service;

    public DailyBalanceServiceTests()
    {
        _mockRepository = new Mock<IDailyBalanceRepository>();
        _mockLockManager = new Mock<IDistributedLockManager>();
        _mockLogger = new Mock<ILogger<DailyBalanceService>>();
        
        // Setup the lock manager to return a disposable lock
        _mockLockManager
            .Setup(m => m.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(Mock.Of<IDisposable>());
            
        _service = new DailyBalanceService(
            _mockRepository.Object, 
            _mockLockManager.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessTransactionAsync_CreditTransaction_ShouldAddCreditToExistingBalance()
    {
        // Arrange
        var merchantId = "test-merchant";
        var transactionDate = DateTime.UtcNow;
        var transactionAmount = 100.50m;

        var existingBalance = new DailyBalance(merchantId, transactionDate, 500.00m);
        var initialClosingBalance = existingBalance.ClosingBalance;

        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = transactionAmount,
            Type = "Credit",
            Description = "Test Credit Transaction",
            Timestamp = transactionDate
        };

        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, transactionDate.Date))
            .ReturnsAsync(existingBalance);
    
        // Mock the TryUpdateWithConcurrencyHandlingAsync to return true
        _mockRepository
            .Setup(r => r.TryUpdateWithConcurrencyHandlingAsync(It.IsAny<DailyBalance>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        await _service.ProcessTransactionAsync(transactionEvent);

        // Assert
        Assert.Equal(initialClosingBalance + transactionAmount, existingBalance.ClosingBalance);
        Assert.Equal(transactionAmount, existingBalance.TotalCredits);
        _mockRepository.Verify(r => r.TryUpdateWithConcurrencyHandlingAsync(It.IsAny<DailyBalance>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionAsync_DebitTransaction_ShouldAddDebitToExistingBalance()
    {
        // Arrange
        var merchantId = "test-merchant";
        var transactionDate = DateTime.UtcNow;
        var transactionAmount = 50.25m;

        var existingBalance = new DailyBalance(merchantId, transactionDate, 500.00m);
        var initialClosingBalance = existingBalance.ClosingBalance;

        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = transactionAmount,
            Type = "Debit",
            Description = "Test Debit Transaction",
            Timestamp = transactionDate
        };

        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, transactionDate.Date))
            .ReturnsAsync(existingBalance);
        
        // Mock the TryUpdateWithConcurrencyHandlingAsync to return true
        _mockRepository
            .Setup(r => r.TryUpdateWithConcurrencyHandlingAsync(It.IsAny<DailyBalance>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        await _service.ProcessTransactionAsync(transactionEvent);

        // Assert
        Assert.Equal(initialClosingBalance - transactionAmount, existingBalance.ClosingBalance);
        Assert.Equal(transactionAmount, existingBalance.TotalDebits);
        _mockRepository.Verify(r => r.TryUpdateWithConcurrencyHandlingAsync(It.IsAny<DailyBalance>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionAsync_FirstTransactionForDay_ShouldCreateNewDailyBalance()
    {
        // Arrange
        var merchantId = "test-merchant";
        var transactionDate = DateTime.UtcNow;
        var transactionAmount = 10;
        var previousDayBalance = new DailyBalance(merchantId, transactionDate.AddDays(-1), 200.00m);
        previousDayBalance.AddCredit(300.00m); // Closing balance is now 500.00m

        var transactionEvent = new TransactionCreatedEvent
        {
            TransactionId = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = transactionAmount,
            Type = "Credit",
            Description = "First Transaction of Day",
            Timestamp = transactionDate
        };

        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, transactionDate.Date))
            .ReturnsAsync((DailyBalance)null);
        _mockRepository
            .Setup(r => r.GetPreviousDayBalanceAsync(merchantId, transactionDate.Date))
            .ReturnsAsync(previousDayBalance);
        _mockRepository
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        DailyBalance capturedBalance = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DailyBalance>()))
            .Callback<DailyBalance>(b => capturedBalance = b)
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessTransactionAsync(transactionEvent);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<DailyBalance>()), Times.Once);
        Assert.NotNull(capturedBalance);
        Assert.Equal(merchantId, capturedBalance.MerchantId);
        Assert.Equal(transactionDate.Date, capturedBalance.Date);
        Assert.Equal(500.00m, capturedBalance.OpeningBalance);
        Assert.Equal(transactionAmount, capturedBalance.TotalCredits);
        Assert.Equal(500.00m + transactionAmount, capturedBalance.ClosingBalance);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionAsync_InvalidTransactionType_ShouldThrowException()
    {
        // Arrange
        var merchantId = "test-merchant";
        var transactionDate = DateTime.UtcNow;
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