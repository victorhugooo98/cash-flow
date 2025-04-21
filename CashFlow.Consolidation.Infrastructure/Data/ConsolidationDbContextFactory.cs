// CashFlow.Consolidation.Infrastructure/Data/ConsolidationDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace CashFlow.Consolidation.Infrastructure.Data;

public class ConsolidationDbContextFactory : IDesignTimeDbContextFactory<ConsolidationDbContext>
{
    public ConsolidationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ConsolidationDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("ConsolidationDatabase") ?? 
            "Server=localhost;Database=CashFlow.Consolidation;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True;");

        return new ConsolidationDbContext(optionsBuilder.Options);
    }
}