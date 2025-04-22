namespace CashFlow.Shared.Resilience;

public class DatabaseRetryOptions
{
    public int RetryCount { get; set; } = 3;
}