using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CashFlow.Transaction.Infrastructure.Data;

public class TransactionDbContextFactory : IDesignTimeDbContextFactory<TransactionDbContext>
{
    public TransactionDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), true)
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"), true)
            .AddEnvironmentVariables()
            .Build();
        
        var optionsBuilder = new DbContextOptionsBuilder<TransactionDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("TransactionDatabase") ??
            "Server=localhost,1433;Database=CashFlow.Transactions;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True;");

        return new TransactionDbContext(optionsBuilder.Options);
    }
}