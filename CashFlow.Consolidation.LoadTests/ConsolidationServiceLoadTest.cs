using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CashFlow.Consolidation.LoadTests;

public class ConsolidationServiceLoadTest
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient = new();

    public ConsolidationServiceLoadTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CanHandle50RequestsPerSecondWith5PercentFailure()
    {
        await SeedTestData("test-merchant");

        // Test parameters
        const int requestsPerSecond = 50;
        const int testDurationSeconds = 30;
        const double minSuccessRate = 95.0;

        // Setup tracking
        var totalRequests = 0;
        var successfulRequests = 0;
        var stopwatch = new Stopwatch();

        // Prepare request data
        var url = "http://localhost:5002/health";

        // Run the test
        stopwatch.Start();

        // Create tasks for all requests
        var tasks = new List<Task>();
        for (var second = 0; second < testDurationSeconds; second++)
        {
            for (var i = 0; i < requestsPerSecond; i++)
                tasks.Add(Task.Run(async () =>
                {
                    Interlocked.Increment(ref totalRequests); // Increment total count for each attempt
                    try
                    {
                        var response = await _httpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode) Interlocked.Increment(ref successfulRequests);
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"Request failed: {ex.Message}");
                    }
                }));

            // Wait 1 second before sending the next batch
            await Task.Delay(1000);
        }

        // Wait for all requests to complete
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Calculate results
        var successRate = (double)successfulRequests / totalRequests * 100;
        var actualRequestsPerSecond = totalRequests / (stopwatch.ElapsedMilliseconds / 1000.0);

        _output.WriteLine($"Total requests: {totalRequests}");
        _output.WriteLine($"Successful requests: {successfulRequests}");
        _output.WriteLine($"Success rate: {successRate:F2}%");
        _output.WriteLine($"Actual requests per second: {actualRequestsPerSecond:F2}");

        // Verify requirements
        Assert.True(successRate >= minSuccessRate,
            $"Success rate ({successRate:F2}%) is below minimum required ({minSuccessRate}%)");
    }

    private async Task SeedTestData(string merchantId)
    {
        var client = new HttpClient();
        // Create some transactions first
        for (var i = 0; i < 5; i++)
        {
            var transaction = new StringContent(
                $"{{\"merchantId\":\"{merchantId}\",\"amount\":100,\"type\":{i % 2},\"description\":\"Test Transaction {i}\"}}",
                System.Text.Encoding.UTF8,
                "application/json");

            await client.PostAsync("http://localhost:5001/api/transactions", transaction);
        }

        // Wait for event processing
        await Task.Delay(3000);
    }
}