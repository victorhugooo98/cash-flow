using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CashFlow.Transaction.API;
using CashFlow.Transaction.Application.DTOs;
using CashFlow.Transaction.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CashFlow.Transaction.IntegrationTests;

public class TransactionApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TransactionApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransaction_ReturnsCreated_WithValidRequest()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            MerchantId = "TestMerchant",
            Amount = 100.50m,
            Type = TransactionType.Credit,
            Description = "Test transaction"
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/transactions", jsonContent);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task GetTransaction_ReturnsTransaction_WhenExists()
    {
        // Arrange - Create a transaction first
        var createRequest = new CreateTransactionRequest
        {
            MerchantId = "TestMerchant",
            Amount = 200m,
            Type = TransactionType.Debit,
            Description = "Test debit transaction"
        };

        var createResponse = await _client.PostAsync("/api/transactions",
            new StringContent(
                JsonSerializer.Serialize(createRequest),
                Encoding.UTF8,
                "application/json"));

        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        // Act
        var getResponse = await _client.GetAsync(location);
        var transaction = await getResponse.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(transaction);
        Assert.Equal("TestMerchant", transaction.MerchantId);
        Assert.Equal(200m, transaction.Amount);
        Assert.Equal("Debit", transaction.Type);
        Assert.Equal("Test debit transaction", transaction.Description);
    }

    [Fact]
    public async Task GetTransactionsByMerchant_ReturnsTransactions_WhenExist()
    {
        // Arrange - Create a unique merchant ID for this test
        var merchantId = $"TestMerchant_{Guid.NewGuid()}";

        // Create two transactions for this merchant
        for (var i = 0; i < 2; i++)
        {
            var createRequest = new CreateTransactionRequest
            {
                MerchantId = merchantId,
                Amount = 100m + i,
                Type = i % 2 == 0 ? TransactionType.Credit : TransactionType.Debit,
                Description = $"Test transaction {i + 1}"
            };

            await _client.PostAsync("/api/transactions",
                new StringContent(
                    JsonSerializer.Serialize(createRequest),
                    Encoding.UTF8,
                    "application/json"));
        }

        // Act
        var response = await _client.GetAsync($"/api/transactions?merchantId={merchantId}");
        var transactions = await response.Content.ReadFromJsonAsync<IEnumerable<TransactionDto>>(_jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(transactions);
        Assert.Equal(2, transactions.Count());
        Assert.All(transactions, t => Assert.Equal(merchantId, t.MerchantId));
    }
}