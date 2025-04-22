using CashFlow.Shared.Exceptions;
using CashFlow.Transaction.Domain.Models;

namespace CashFlow.Transaction.UnitTests;

public class TransactionTests
{
    [Fact]
    public void CreateTransaction_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var merchantId = "merchant123";
        var amount = 100.50m;
        var type = TransactionType.Credit;
        var description = "Test transaction";

        // Act
        var transaction = Domain.Models.Transaction.Create(merchantId, amount, type, description);

        // Assert
        Assert.Equal(merchantId, transaction.MerchantId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(type, transaction.Type);
        Assert.Equal(description, transaction.Description);
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.True(DateTime.UtcNow.Subtract(transaction.Timestamp).TotalSeconds < 5);
    }

    [Theory]
    [InlineData("", 100, TransactionType.Credit, "Description", "Merchant ID cannot be empty")]
    [InlineData("merchant123", 0, TransactionType.Credit, "Description", "Amount must be greater than zero")]
    [InlineData("merchant123", -10, TransactionType.Credit, "Description", "Amount must be greater than zero")]
    [InlineData("merchant123", 100, TransactionType.Credit, "", "Description cannot be empty")]
    public void CreateTransaction_WithInvalidParameters_ShouldThrowValidationException(
        string merchantId, decimal amount, TransactionType type, string description, string expectedErrorMessage)
    {
        // Act & Assert
        var exception = Assert.Throws<TransactionValidationException>(
            () => Domain.Models.Transaction.Create(merchantId, amount, type, description));

        Assert.Contains(expectedErrorMessage, exception.ValidationErrors);
    }

    [Fact]
    public void TransactionValidationException_ShouldContainAllValidationErrors()
    {
        // Arrange & Act
        var exception = Assert.Throws<TransactionValidationException>(
            () => Domain.Models.Transaction.Create("", -5, TransactionType.Credit, ""));

        // Assert
        Assert.Equal(3, exception.ValidationErrors.Count());
        Assert.Contains("Merchant ID cannot be empty", exception.ValidationErrors);
        Assert.Contains("Amount must be greater than zero", exception.ValidationErrors);
        Assert.Contains("Description cannot be empty", exception.ValidationErrors);
    }
}