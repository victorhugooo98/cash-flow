using System.Net;
using Xunit.Abstractions;

namespace CashFlow.Consolidation.LoadTests;

/// <summary>
/// A simple manual load test for the Consolidation Service
/// </summary>
public class ConsolidationServiceLoadTest
{
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "http://localhost:5002"; // Adjust as needed

    public ConsolidationServiceLoadTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task LoadTest_ConsolidationService_ShouldHandleHighLoad()
    {
        // Define specific merchant ID and date for testing
        var merchantId = "loadtest-merchant";
        var date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        // Create the HTTP client
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        // Track results
        var totalRequests = 0;
        var successfulRequests = 0;
        var stopwatch = new System.Diagnostics.Stopwatch();

        // Start the test
        stopwatch.Start();

        // Send requests at a rate of approximately 50 per second for 10 seconds
        var tasks = new List<Task>();

        for (var i = 0; i < 500; i++) // 50 requests/sec * 10 seconds = 500 requests
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var url = $"{BaseUrl}/api/balances/daily?merchantId={merchantId}&date={date}";
                    var response = await httpClient.GetAsync(url);

                    Interlocked.Increment(ref totalRequests);

                    if (response.StatusCode == HttpStatusCode.OK ||
                        response.StatusCode == HttpStatusCode.NotFound) // Consider NotFound a valid response
                        Interlocked.Increment(ref successfulRequests);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref totalRequests);
                    _output.WriteLine("Request failed: " + ex.Message);
                }
            }));

            // Add a small delay to distribute requests (not exactly 50/sec but close enough for testing)
            if (i % 50 == 0) await Task.Delay(1000);
        }

        // Wait for all requests to complete
        await Task.WhenAll(tasks);

        // Stop timing
        stopwatch.Stop();

        // Calculate metrics
        var durationSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
        var requestsPerSecond = totalRequests / durationSeconds;
        var successRate = (double)successfulRequests / totalRequests;

        // Output results
        _output.WriteLine("Total requests: " + totalRequests);
        _output.WriteLine("Successful requests: " + successfulRequests);
        _output.WriteLine("Duration: " + durationSeconds.ToString("F1") + " seconds");
        _output.WriteLine("Requests per second: " + requestsPerSecond.ToString("F1"));
        _output.WriteLine("Success rate: " + (successRate * 100).ToString("F1") + "%");

        // Assertions
        Assert.True(successRate >= 0.95, "Success rate should be at least 95%");
        Assert.True(requestsPerSecond >= 47, "Should handle at least 47 requests per second");
    }
}