using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Consolidation.Infrastructure.Data;
using CashFlow.Consolidation.Infrastructure.Messaging;
using CashFlow.Consolidation.Infrastructure.Repositories;
using CashFlow.Consolidation.Infrastructure.Resilience;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<ConsolidationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConsolidationDatabase")));

// Add repositories
builder.Services.AddScoped<IDailyBalanceRepository, DailyBalanceRepository>();

// Add resilience policies
builder.Services.AddSingleton<CircuitBreakerPolicyProvider>();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TransactionEventConsumer>(cfg =>
    {
        // Configure retry policy
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

        // Configure circuit breaker
        cfg.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 15; // 15% of messages failing
            cb.ActiveThreshold = 10; // 10 messages in the active window
            cb.ResetInterval = TimeSpan.FromMinutes(5);
        });
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        // Configure the consumer endpoint
        cfg.ReceiveEndpoint("cashflow-transaction-events", e =>
        {
            // Set concurrency limit to handle high load
            e.PrefetchCount = 20;

            // Configure consumer
            e.ConfigureConsumer<TransactionEventConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ConsolidationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();