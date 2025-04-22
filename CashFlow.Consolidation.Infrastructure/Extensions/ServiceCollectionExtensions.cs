using CashFlow.Consolidation.Application.Interfaces;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Consolidation.Infrastructure.Data;
using CashFlow.Consolidation.Infrastructure.Messaging;
using CashFlow.Consolidation.Infrastructure.Repositories;
using CashFlow.Consolidation.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Consolidation.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<ConsolidationDbContext>((provider, options) =>
        {
            var connectionString = configuration.GetConnectionString("ConsolidationDatabase");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    3,
                    TimeSpan.FromSeconds(5),
                    null);
                
                // Set command timeout to avoid long-running queries
                sqlOptions.CommandTimeout(30);
            });
        });
        
        // Register repositories
        services.AddScoped<IDailyBalanceRepository, DailyBalanceRepository>();
        
        // Register services
        services.AddScoped<IProcessedTransactionsIdempotencyService, ProcessedTransactionsIdempotencyService>();
        services.AddScoped<IProcessedTransactionsIdempotencyService, ProcessedTransactionsIdempotencyService>();

        services.AddSingleton<IDistributedLockManager, InMemoryDistributedLockManager>();
        
        return services;
    }
}