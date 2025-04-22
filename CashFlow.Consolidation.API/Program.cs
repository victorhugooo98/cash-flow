using CashFlow.Consolidation.Application.Behaviors;
using CashFlow.Consolidation.Application.Interfaces;
using CashFlow.Consolidation.Application.Queries;
using CashFlow.Consolidation.Application.Services;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Consolidation.Infrastructure.Data;
using CashFlow.Consolidation.Infrastructure.Messaging;
using CashFlow.Consolidation.Infrastructure.Repositories;
using CashFlow.Consolidation.Infrastructure.Resilience;
using CashFlow.Consolidation.Infrastructure.Services;
using CashFlow.Shared.Middleware;
using CashFlow.Shared.Resilience;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetDailyBalanceQuery).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(GetDailyBalanceQuery).Assembly);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CashFlow Consolidation API", Version = "v1" });
});

// Add DbContext
builder.Services.AddDbContext<ConsolidationDbContext>((provider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("ConsolidationDatabase");
    options.UseSqlServer(connectionString, sqlOptions => 
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

// Add repositories
builder.Services.AddScoped<IDailyBalanceRepository, DailyBalanceRepository>();

builder.Services.AddScoped<IDailyBalanceService, DailyBalanceService>();
builder.Services.AddScoped<IBalanceHistoryService, BalanceHistoryService>();

// Add resilience policies
builder.Services.Configure<CircuitBreakerOptions>(builder.Configuration.GetSection("CircuitBreaker"));
builder.Services.Configure<DatabaseRetryOptions>(builder.Configuration.GetSection("DatabaseRetry"));
builder.Services.AddSingleton<CircuitBreakerPolicyProvider>();
builder.Services.AddSingleton<DatabasePolicyProvider>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Register the consumer
    x.AddConsumer<TransactionEventConsumer>(cfg =>
    {
        // Configure retry policy with exponential backoff
        cfg.UseMessageRetry(r =>
        {
            r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
        });

        // Configure circuit breaker
        cfg.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 15; // 15% of messages failing
            cb.ActiveThreshold = 10; // 10 messages in the active window
            cb.ResetInterval = TimeSpan.FromMinutes(5);
        });
    });

    // Configure RabbitMQ
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
        var host = rabbitMqConfig["Host"] ?? "localhost";
        var username = rabbitMqConfig["Username"] ?? "guest";
        var password = rabbitMqConfig["Password"] ?? "guest";

        // Configure RabbitMQ connection
        cfg.Host(new Uri($"rabbitmq://{host}"), h =>
        {
            h.Username(username);
            h.Password(password);
        });

        // Configure the consumer endpoint
        cfg.ReceiveEndpoint("cashflow-transaction-events", e =>
        {
            // Set concurrency limit to handle high load
            e.PrefetchCount = 50;

            // Configure error handling
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

            // Configure error queue instead of dead letter queue
            e.UseMessageRetry(r => r.Immediate(3));
            e.UseInMemoryOutbox();

            // Configure consumer
            e.ConfigureConsumer<TransactionEventConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("db-check", () => HealthCheckResult.Healthy(), ["ready"])
    .AddCheck("rabbitmq-check", () => HealthCheckResult.Healthy(), ["ready"]);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CashFlow Consolidation API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ConsolidationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();