using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace CashFlow.Consolidation.LoadTests;

public class ConsolidationServiceLoadTest
{
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "http://localhost:5002"; // Adjust as needed
    
    public ConsolidationServiceLoadTest(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void LoadTest_ConsolidationService_ShouldHandleHighLoad()
    {
        // Define specific merchant ID and date for testing
        var merchantId = "loadtest-merchant";
        var date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        
        // Create HTTP client factory for NBomber
        var httpFactory = HttpClientFactory.Create();
        
        // Define step to request daily balance
        var step = Step.Create("request_daily_balance", httpFactory, async context =>
        {
            var request = WebRequestMethods.Http.CreateRequest("GET", $"{BaseUrl}/api/balances/daily?merchantId={merchantId}&date={date}")
                .WithHeader("Accept", "application/json");
                
            var response = await WebRequestMethods.Http.Send(request, context);
                
            return response.StatusCode switch
            {
                HttpStatusCode.OK => response,
                HttpStatusCode.NotFound => response, // Consider NotFound a valid response for load test
                _ => Response.Fail(statusCode: (int)response.StatusCode)
            };
        });
        
        // Define the test scenario
        var scenario = ScenarioBuilder.CreateScenario("consolidation_api_load_test", step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                Simulation.Inject(rate: 50, // 50 requests per second
                                 interval: TimeSpan.FromSeconds(1),
                                 during: TimeSpan.FromMinutes(1))
            );
            
        // Run the test
        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
            
        // Output the results
        _output.WriteLine($"Request count: {result.AllRequestCount}");
        _output.WriteLine($"Failed request count: {result.FailCount}");
        _output.WriteLine($"RPS: {result.ScenarioStats[0].RPS}");
        _output.WriteLine($"Mean response time: {result.ScenarioStats[0].MeanResponseTime} ms");
        _output.WriteLine($"99th percentile: {result.ScenarioStats[0].Percentile99} ms");
            
        // Assert that failure rate is less than 5%
        var failureRate = (double)result.FailCount / result.AllRequestCount;
        Assert.True(failureRate <= 0.05, $"Failure rate of {failureRate:P2} exceeds the maximum allowed 5%");
            
        // Assert that we actually achieved 50 RPS (with some margin)
        Assert.True(result.ScenarioStats[0].RPS >= 47, $"RPS of {result.ScenarioStats[0].RPS} is less than expected minimum of 47");
    }
}