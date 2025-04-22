using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CashFlow.Consolidation.Application.DTOs;
using Xunit.Abstractions;

namespace CashFlow.Consolidation.LoadTests;

/// <summary>
/// Load test for transaction event processing
/// </summary>
public class TransactionEventLoadTest
{
    private readonly ITestOutputHelper _output;
    private const string TransactionApiUrl = "http://localhost:5001"; // Transaction API
    private const string ConsolidationApiUrl = "http://localhost:5002"; // Consolidation API

    public TransactionEventLoadTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task LoadTest_TransactionEventProcessing_ShouldHandleHighVolume()
    {
        // Define test parameters
        var merchantId = $"loadtest-merchant-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        var numberOfTransactions = 10;
        var transactionAmount = 10.50m;

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        // Track results
        var successfulTransactions = 0;
        var successfulBalanceChecks = 0;
        var stopwatch = new Stopwatch();

        // Start the test
        stopwatch.Start();

        // Step 1: Create multiple transactions in parallel
        _output.WriteLine($"Creating {numberOfTransactions} transactions for merchant {merchantId}...");

        var transactionTasks = new List<Task>();
        for (var i = 0; i < numberOfTransactions; i++)
        {
            var isCredit = i % 2 == 0;
            var request = new
            {
                merchantId,
                amount = transactionAmount,
                type = isCredit ? 0 : 1, // 0 = Credit, 1 = Debit
                description = $"Load Test Transaction {i + 1}"
            };

            transactionTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var content = new StringContent(
                        JsonSerializer.Serialize(request),
                        Encoding.UTF8,
                        "application/json");

                    var response = await httpClient.PostAsync($"{TransactionApiUrl}/api/transactions", content);

                    if (response.IsSuccessStatusCode)
                        Interlocked.Increment(ref successfulTransactions);
                    else
                        _output.WriteLine(
                            $"Transaction creation failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Transaction creation exception: {ex.Message}");
                }
            }));

            // Introduce a small delay between batches to prevent throttling
            if (i % 10 == 0) await Task.Delay(500);
        }

        // Wait for all transactions to be created
        await Task.WhenAll(transactionTasks);

        // Step 2: Wait for consolidation to process events
        _output.WriteLine("Waiting for event processing...");
        await Task.Delay(10000); // Give some time for events to be processed

        // Step 3: Check daily balance to verify transaction processing
        var maxAttempts = 20;
        var date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var balanceResponse = await httpClient.GetAsync(
                    $"{ConsolidationApiUrl}/api/dailybalances/daily?merchantId={merchantId}&date={date}");

                Console.WriteLine($"{ConsolidationApiUrl}/api/dailybalances/daily?merchantId={merchantId}&date={date}");
                
                if (balanceResponse.IsSuccessStatusCode)
                {
                    var balance = await balanceResponse.Content.ReadFromJsonAsync<DailyBalanceResponse>();
                    var totalCredits = balance?.TotalCredits;
                    var totalDebits = balance?.TotalDebits;
                    var closingBalance = balance?.ClosingBalance;

                    _output.WriteLine(
                        $"Balance found: Credits={totalCredits}, Debits={totalDebits}, Closing={closingBalance}");

                    // Verify correct number of transactions were processed
                    var expectedCredits = numberOfTransactions / 2 * transactionAmount;
                    var expectedDebits = numberOfTransactions / 2 * transactionAmount;

                    _output.WriteLine(
                        $"[Attempt {attempt}] Expected: {expectedCredits}, {expectedDebits} | Got: {totalCredits}, {totalDebits}");

                    if (Math.Abs((decimal)(totalCredits - expectedCredits)!) < 0.01m &&
                        Math.Abs((decimal)(totalDebits - expectedDebits)!) < 0.01m)
                    {
                        successfulBalanceChecks = 1;
                        break;
                    }

                    _output.WriteLine(
                        $"Balance not yet consistent. Expected Credits={expectedCredits}, Debits={expectedDebits}");
                }
                else if (balanceResponse.StatusCode == HttpStatusCode.NotFound && attempt < maxAttempts)
                {
                    // Balance record might not be created yet
                    _output.WriteLine($"Balance not found on attempt {attempt}. Waiting for processing...");
                }
                else
                {
                    _output.WriteLine(
                        $"Balance check failed: {balanceResponse.StatusCode} - {await balanceResponse.Content.ReadAsStringAsync()}");
                    break;
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Balance check exception: {ex.Message}");
                break;
            }

            // Wait before next attempt
            await Task.Delay(5000);
        }

        // Stop timing
        stopwatch.Stop();

        // Calculate metrics
        var durationSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
        var transactionsPerSecond = successfulTransactions / durationSeconds;

        // Output results
        _output.WriteLine($"Test completed in {durationSeconds:F1} seconds");
        _output.WriteLine($"Successful transaction creations: {successfulTransactions} of {numberOfTransactions}");
        _output.WriteLine($"Transaction creation rate: {transactionsPerSecond:F1} per second");
        _output.WriteLine($"Balance verification: {(successfulBalanceChecks > 0 ? "Success" : "Failed")}");

        // Assertions
        Assert.True(successfulTransactions >= numberOfTransactions * 0.95,
            "At least 95% of transactions should be created successfully");
        Assert.True(successfulBalanceChecks > 0, "Final balance check should be successful");
    }
}