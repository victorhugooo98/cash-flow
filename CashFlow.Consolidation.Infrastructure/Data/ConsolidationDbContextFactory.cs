using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CashFlow.Consolidation.Infrastructure.Data;

public class ConsolidationDbContextFactory : IDesignTimeDbContextFactory<ConsolidationDbContext>
{
    public ConsolidationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), true)
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"), true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ConsolidationDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("ConsolidationDatabase") ??
            "Server=localhost;Database=CashFlow.Consolidation;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True;");

        return new ConsolidationDbContext(optionsBuilder.Options);
    }
}