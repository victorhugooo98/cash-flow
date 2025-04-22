using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CashFlow.Consolidation.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace CashFlow.Consolidation.IntegrationTests;

public class ConsolidationApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConsolidationApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetDailyBalance_ReturnsNotFound_WhenBalanceDoesNotExist()
    {
        // Arrange
        var merchantId = Guid.NewGuid().ToString();
        var date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/api/dailybalances/daily?merchantId={merchantId}&date={date}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BalanceHistory_ReturnsNotFound_WhenNoBalancesExist()
    {
        // Arrange
        var merchantId = Guid.NewGuid().ToString();
        var startDate = DateTime.UtcNow.AddDays(-30).Date.ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync(
            $"/api/balancehistory?merchantId={merchantId}&startDate={startDate}&endDate={endDate}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BalanceHistory_ReturnsBadRequest_WhenInvalidDateRange()
    {
        // Arrange
        var merchantId = "test-merchant";
        var startDate = DateTime.UtcNow.AddDays(10).Date.ToString("yyyy-MM-dd"); // Future date
        var endDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync(
            $"/api/balancehistory?merchantId={merchantId}&startDate={startDate}&endDate={endDate}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BalanceTrends_ReturnsNotFound_WhenNoBalancesExist()
    {
        // Arrange
        var merchantId = Guid.NewGuid().ToString();
        var startDate = DateTime.UtcNow.AddDays(-90).Date.ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync(
            $"/api/balancehistory/trends?merchantId={merchantId}&startDate={startDate}&endDate={endDate}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // This test would require actual data in the database
    // In a real environment, we would seed test data first
    [Fact(Skip = "Requires seeded test data")]
    public async Task GetDailyBalance_ReturnsBalance_WhenExists()
    {
        // Arrange
        var merchantId = "test-merchant";
        var date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/api/dailybalances/daily?merchantId={merchantId}&date={date}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var balance = await response.Content.ReadFromJsonAsync<DailyBalanceResponse>(_jsonOptions);
        Assert.NotNull(balance);
        Assert.Equal(merchantId, balance.MerchantId);
    }
}