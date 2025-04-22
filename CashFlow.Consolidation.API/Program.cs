using System.Globalization;
using CashFlow.Consolidation.Application.Behaviors;
using CashFlow.Consolidation.Application.DTOs;
using CashFlow.Consolidation.Application.Interfaces;
using CashFlow.Consolidation.Application.Queries;
using CashFlow.Consolidation.Application.Services;
using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Consolidation.Infrastructure.Data;
using CashFlow.Consolidation.Infrastructure.Extensions;
using CashFlow.Consolidation.Infrastructure.Messaging;
using CashFlow.Consolidation.Infrastructure.Repositories;
using CashFlow.Consolidation.Infrastructure.Services;
using CashFlow.Shared.Middleware;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;
using Serilog;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
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

// Add Infrastructure services (using the extension method)
        builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Application services
        builder.Services.AddScoped<IDailyBalanceService, DailyBalanceService>();
        builder.Services.AddScoped<IBalanceHistoryService, BalanceHistoryService>();
        builder.Services.AddSingleton<IDistributedLockManager, InMemoryDistributedLockManager>();

// Configure MassTransit with RabbitMQ
        builder.Services.AddMassTransit(x =>
        {
            // Register the consumer
            x.AddConsumer<TransactionEventConsumer>(cfg =>
            {
                // Configure retry policy with exponential backoff
                cfg.UseMessageRetry(r => r.Exponential(
                    5,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(1)));

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
                    e.PrefetchCount = 20; // Lower to reduce contention

                    // Configure error handling
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                    // Add deadletter queue for failed messages
                    e.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15),
                        TimeSpan.FromSeconds(30)));

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

        builder.WebHost.ConfigureKestrel(serverOptions => { serverOptions.ListenAnyIP(80); });

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

        app.MapGroup("/api/dailybalances")
            .MapGet("/", async (IMediator mediator, string merchantId, ILogger<Program> logger,
                DateTime? startDate = null, DateTime? endDate = null) =>
            {
                try
                {
                    var currentDate = DateTime.UtcNow.Date;
                    startDate ??= currentDate.AddDays(-30);
                    endDate ??= currentDate;

                    var balances = new List<DailyBalanceDto>();
                    var dailyBalanceRepository = app.Services.GetRequiredService<IDailyBalanceRepository>();
                    var currentDatePointer = startDate.Value.Date;

                    while (currentDatePointer <= endDate.Value.Date)
                    {
                        var balance =
                            await dailyBalanceRepository.GetByMerchantAndDateAsync(merchantId, currentDatePointer);
                        if (balance != null)
                            balances.Add(MapToDto(balance));
                        currentDatePointer = currentDatePointer.AddDays(1);
                    }

                    if (!balances.Any())
                        return Results.NotFound(
                            $"No balance records found for merchant {merchantId} in the specified date range");

                    var responses = balances.Select(DailyBalanceResponse.FromDto).ToList();
                    return Results.Ok(responses);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error retrieving daily balances for merchant {MerchantId} from {StartDate} to {EndDate}",
                        merchantId, startDate, endDate);
                    return Results.StatusCode(500);
                }
            });

        app.MapGroup("/api/dailybalances")
            .MapGet("/daily", async (IMediator mediator, string merchantId, DateTime date, ILogger<Program> logger) =>
            {
                try
                {
                    var query = new GetDailyBalanceQuery
                    {
                        MerchantId = merchantId,
                        Date = date
                    };

                    var balance = await mediator.Send(query);
                    if (balance == null)
                        return Results.NotFound(
                            $"No balance record found for merchant {merchantId} on {date:yyyy-MM-dd}");

                    return Results.Ok(DailyBalanceResponse.FromDto(balance));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving daily balance for merchant {MerchantId} on {Date}",
                        merchantId, date);
                    return Results.StatusCode(500);
                }
            });

        app.MapGroup("/api/dailybalances")
            .MapGet("/summary", async (string merchantId, IDailyBalanceRepository balanceRepository,
                ILogger<Program> logger, DateTime? startDate = null, DateTime? endDate = null) =>
            {
                try
                {
                    var currentDate = DateTime.UtcNow.Date;
                    startDate ??= currentDate.AddDays(-30);
                    endDate ??= currentDate;

                    var balances = new List<DailyBalance>();
                    var currentDatePointer = startDate.Value.Date;

                    while (currentDatePointer <= endDate.Value.Date)
                    {
                        var balance = await balanceRepository.GetByMerchantAndDateAsync(merchantId, currentDatePointer);
                        if (balance != null) balances.Add(balance);
                        currentDatePointer = currentDatePointer.AddDays(1);
                    }

                    var summary = new
                    {
                        MerchantId = merchantId,
                        Period = new
                        {
                            StartDate = startDate.Value.ToString("yyyy-MM-dd"),
                            EndDate = endDate.Value.ToString("yyyy-MM-dd")
                        },
                        TotalCredits = balances.Sum(b => b.TotalCredits),
                        TotalDebits = balances.Sum(b => b.TotalDebits),
                        NetChange = balances.Sum(b => b.TotalCredits) - balances.Sum(b => b.TotalDebits),
                        StartingBalance = balances.OrderBy(b => b.Date).FirstOrDefault()?.OpeningBalance ?? 0,
                        EndingBalance = balances.OrderByDescending(b => b.Date).FirstOrDefault()?.ClosingBalance ?? 0,
                        DaysWithTransactions = balances.Count
                    };

                    return Results.Ok(summary);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error generating balance summary for merchant {MerchantId} from {StartDate} to {EndDate}",
                        merchantId, startDate, endDate);
                    return Results.StatusCode(500);
                }
            });

// Balance History endpoints
        app.MapGroup("/api/balancehistory")
            .MapGet("/", async (IMediator mediator, string merchantId, ILogger<Program> logger,
                DateTime? startDate = null, DateTime? endDate = null) =>
            {
                try
                {
                    var currentDate = DateTime.UtcNow.Date;
                    startDate ??= currentDate.AddDays(-30);
                    endDate ??= currentDate;

                    var query = new GetBalanceHistoryQuery
                    {
                        MerchantId = merchantId,
                        StartDate = startDate.Value,
                        EndDate = endDate.Value
                    };

                    var balanceHistory = await mediator.Send(query);
                    if (balanceHistory == null || !balanceHistory.Entries.Any())
                        return Results.NotFound(
                            $"No balance records found for merchant {merchantId} in the specified date range");

                    return Results.Ok(BalanceHistoryResponse.FromDto(balanceHistory));
                }
                catch (ValidationException ex)
                {
                    logger.LogWarning(ex, "Validation error for balance history request");
                    return Results.BadRequest(new
                    {
                        Errors = ex.Errors.Select(e => new { Property = e.PropertyName, Error = e.ErrorMessage })
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving balance history for merchant {MerchantId}", merchantId);
                    return Results.StatusCode(500);
                }
            });

        app.MapGroup("/api/balancehistory")
            .MapGet("/trends", async (IMediator mediator, string merchantId, ILogger<Program> logger,
                DateTime? startDate = null, DateTime? endDate = null) =>
            {
                try
                {
                    var currentDate = DateTime.UtcNow.Date;
                    startDate ??= currentDate.AddDays(-90);
                    endDate ??= currentDate;

                    var query = new GetBalanceHistoryQuery
                    {
                        MerchantId = merchantId,
                        StartDate = startDate.Value,
                        EndDate = endDate.Value
                    };

                    var balanceHistory = await mediator.Send(query);
                    if (balanceHistory == null || !balanceHistory.Entries.Any())
                        return Results.NotFound(
                            $"No balance records found for merchant {merchantId} in the specified date range");

                    // Calculate weekly and monthly trends (as in your original controller)
                    var weeklyTrends = CalculateWeeklyTrends(balanceHistory);
                    var monthlyTrends = CalculateMonthlyTrends(balanceHistory);

                    var response = new
                    {
                        MerchantId = merchantId,
                        Period = $"{startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}",
                        Overview = new
                        {
                            TotalTransactions = balanceHistory.Entries.Count,
                            balanceHistory.TotalCredits,
                            balanceHistory.TotalDebits,
                            balanceHistory.NetChange,
                            balanceHistory.InitialBalance,
                            balanceHistory.FinalBalance,
                            DailyBalanceTrend = balanceHistory.BalanceTrend
                        },
                        WeeklyTrends = weeklyTrends,
                        MonthlyTrends = monthlyTrends
                    };

                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving balance trends for merchant {MerchantId}", merchantId);
                    return Results.StatusCode(500);
                }
            });

// Health check endpoints
        app.MapGet("/health", () => Results.Ok("Service is running"));
        app.MapGet("/health/ready", async (HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync(registration => registration.Tags.Contains("ready"));
            return report.Status == HealthStatus.Healthy
                ? Results.Ok("Service is ready")
                : Results.StatusCode(503);
        });
        app.MapGet("/health/live", () => Results.Ok("Service is running"));

// Ensure database is created and migrations are applied
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ConsolidationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        app.Run();
    }

    private static List<object> CalculateWeeklyTrends(BalanceHistoryDto history)
    {
        return history.Entries
            .GroupBy(e => GetWeekNumber(e.Date))
            .Select(g => new
            {
                WeekStarting = GetFirstDayOfWeek(g.First().Date).ToString("yyyy-MM-dd"),
                WeekEnding = GetFirstDayOfWeek(g.First().Date).AddDays(6).ToString("yyyy-MM-dd"),
                TotalCredits = g.Sum(e => e.TotalCredits),
                TotalDebits = g.Sum(e => e.TotalDebits),
                NetChange = g.Sum(e => e.TotalCredits - e.TotalDebits),
                StartBalance = g.First().OpeningBalance,
                EndBalance = g.Last().ClosingBalance
            })
            .Cast<object>()
            .ToList();
    }

    private static DailyBalanceDto MapToDto(DailyBalance balance)
    {
        return new DailyBalanceDto
        {
            Id = balance.Id,
            MerchantId = balance.MerchantId,
            Date = balance.Date,
            OpeningBalance = balance.OpeningBalance,
            TotalCredits = balance.TotalCredits,
            TotalDebits = balance.TotalDebits,
            ClosingBalance = balance.ClosingBalance
        };
    }

    private static int GetWeekNumber(DateTime date)
    {
        // ISO 8601 week number calculation
        var day = (int)CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date.AddDays(day == 0 ? -6 : 1 - day),
            CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
    }

    private static DateTime GetFirstDayOfWeek(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        return date.AddDays(day == 0 ? -6 : 1 - day); // First day is Monday
    }

    private static List<object> CalculateMonthlyTrends(BalanceHistoryDto history)
    {
        return history.Entries
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new
            {
                Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("yyyy-MM"),
                TotalCredits = g.Sum(e => e.TotalCredits),
                TotalDebits = g.Sum(e => e.TotalDebits),
                NetChange = g.Sum(e => e.TotalCredits - e.TotalDebits),
                StartBalance = g.OrderBy(e => e.Date).First().OpeningBalance,
                EndBalance = g.OrderBy(e => e.Date).Last().ClosingBalance
            })
            .Cast<object>()
            .ToList();
    }
}