using CashFlow.Consolidation.Application.Services;
using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace CashFlow.Consolidation.UnitTests.Services;

public class BalanceHistoryServiceTests
{
    private readonly Mock<IDailyBalanceRepository> _mockRepository;
    private readonly Mock<ILogger<BalanceHistoryService>> _mockLogger;
    private readonly BalanceHistoryService _service;

    public BalanceHistoryServiceTests()
    {
        _mockRepository = new Mock<IDailyBalanceRepository>();
        _mockLogger = new Mock<ILogger<BalanceHistoryService>>();
        _service = new BalanceHistoryService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBalanceHistoryAsync_NoData_ReturnsEmptyHistory()
    {
        // Arrange
        var merchantId = "test-merchant";
        var startDate = DateTime.UtcNow.Date.AddDays(-7);
        var endDate = DateTime.UtcNow.Date;
        
        // Mock repository to return null for any date in the range
        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync((DailyBalance)null);
        
        // Act
        var result = await _service.GetBalanceHistoryAsync(merchantId, startDate, endDate);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(merchantId, result.MerchantId);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.Empty(result.Entries);
        Assert.Equal(0, result.TotalCredits);
        Assert.Equal(0, result.TotalDebits);
        Assert.Equal(0, result.NetChange);
    }
    
    [Fact]
    public async Task GetBalanceHistoryAsync_WithData_ReturnsHistoryWithEntries()
    {
        // Arrange
        var merchantId = "test-merchant";
        var startDate = DateTime.UtcNow.Date.AddDays(-2);
        var endDate = DateTime.UtcNow.Date;
        
        // Create test data for three days
        var day1 = startDate;
        var day2 = startDate.AddDays(1);
        var day3 = endDate;
        
        var balance1 = new DailyBalance(merchantId, day1, 1000m);
        balance1.AddCredit(500m);
        balance1.AddDebit(200m);
        
        var balance2 = new DailyBalance(merchantId, day2, balance1.ClosingBalance);
        balance2.AddCredit(300m);
        balance2.AddDebit(400m);
        
        var balance3 = new DailyBalance(merchantId, day3, balance2.ClosingBalance);
        balance3.AddCredit(700m);
        balance3.AddDebit(100m);
        
        // Setup repository mock
        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, day1))
            .ReturnsAsync(balance1);
        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, day2))
            .ReturnsAsync(balance2);
        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, day3))
            .ReturnsAsync(balance3);
        
        // Act
        var result = await _service.GetBalanceHistoryAsync(merchantId, startDate, endDate);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(merchantId, result.MerchantId);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        
        // Check entries
        Assert.Equal(3, result.Entries.Count);
        
        // Check summary values
        Assert.Equal(1500m, result.TotalCredits); // 500 + 300 + 700
        Assert.Equal(700m, result.TotalDebits);   // 200 + 400 + 100
        Assert.Equal(800m, result.NetChange);     // 1500 - 700
        Assert.Equal(1000m, result.InitialBalance); // Opening balance of first day
        Assert.Equal(1800m, result.FinalBalance);   // Closing balance of last day
        Assert.Equal(3, result.DaysWithActivity);
        Assert.Equal(500m, result.AverageDailyCredits); // 1500 / 3
        Assert.Equal(233.33m, Math.Round(result.AverageDailyDebits, 2)); // 700 / 3 ≈ 233.33
        
        // Trend calculation: (finalBalance - initialBalance) / days
        // (1800 - 1000) / 3 = 266.67
        Assert.Equal(266.67m, Math.Round(result.BalanceTrend, 2));
    }
    
    [Fact]
    public async Task GetBalanceHistoryAsync_SingleDay_CalculatesCorrectTrend()
    {
        // Arrange
        var merchantId = "test-merchant";
        var singleDate = DateTime.UtcNow.Date;
        
        var balance = new DailyBalance(merchantId, singleDate, 1000m);
        balance.AddCredit(500m);
        balance.AddDebit(200m);
        
        _mockRepository
            .Setup(r => r.GetByMerchantAndDateAsync(merchantId, singleDate))
            .ReturnsAsync(balance);
        
        // Act
        var result = await _service.GetBalanceHistoryAsync(merchantId, singleDate, singleDate);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Entries);
        Assert.Equal(1000m, result.InitialBalance);
        Assert.Equal(1300m, result.FinalBalance);
        Assert.Equal(0m, result.BalanceTrend); // No trend for a single day
    }
}