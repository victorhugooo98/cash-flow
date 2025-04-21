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
        var transaction = new Domain.Models.Transaction(merchantId, amount, type, description);

        // Assert
        Assert.Equal(merchantId, transaction.MerchantId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(type, transaction.Type);
        Assert.Equal(description, transaction.Description);
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.True(DateTime.UtcNow.Subtract(transaction.Timestamp).TotalSeconds < 5);
    }

    [Theory]
    [InlineData("", 100, TransactionType.Credit, "Description")]
    [InlineData("merchant123", 0, TransactionType.Credit, "Description")]
    [InlineData("merchant123", -10, TransactionType.Credit, "Description")]
    [InlineData("merchant123", 100, TransactionType.Credit, "")]
    public void CreateTransaction_WithInvalidParameters_ShouldThrowException(
        string merchantId, decimal amount, TransactionType type, string description)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Domain.Models.Transaction(merchantId, amount, type, description));
    }
}