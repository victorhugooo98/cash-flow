namespace CashFlow.Shared.Exceptions;

public class TransactionValidationException : Exception
{
    public IEnumerable<string> ValidationErrors { get; }

    public TransactionValidationException(IEnumerable<string> validationErrors)
        : base($"Transaction validation failed: {string.Join(", ", validationErrors)}")
    {
        ValidationErrors = validationErrors;
    }
}