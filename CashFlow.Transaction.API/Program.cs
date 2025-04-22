using CashFlow.Shared.Middleware;
using CashFlow.Transaction.Application.Behaviors;
using CashFlow.Transaction.Application.Commands.CreateTransaction;
using CashFlow.Transaction.Application.DTOs;
using CashFlow.Transaction.Application.Events;
using CashFlow.Transaction.Application.Queries.GetTransactionById;
using CashFlow.Transaction.Application.Queries.GetTransactionsByMerchant;
using CashFlow.Transaction.Domain.Repositories;
using CashFlow.Transaction.Infrastructure.Data;
using CashFlow.Transaction.Infrastructure.Messaging;
using CashFlow.Transaction.Infrastructure.Repositories;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace CashFlow.Transaction.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();

// Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

// Add Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "CashFlow Transaction API", Version = "v1" });
        });

// Add MediatR
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateTransactionCommand).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

// Add DbContext
        builder.Services.AddDbContext<TransactionDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("TransactionDatabase")));

// Add repositories
        builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

        builder.Services.AddScoped<TransactionDbContext>();

// Configure MassTransit with RabbitMQ
        builder.Services.AddMassTransit(x =>
        {
            // Configure the RabbitMQ transport
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

                // Configure message retry
                cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(1)));

                cfg.ConfigureEndpoints(context);
            });
        });

// Add event publisher
        builder.Services.AddScoped<ITransactionEventPublisher, TransactionEventPublisher>();

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
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CashFlow Transaction API v1"));
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.MapGroup("/api/transactions")
            .MapPost("/", async (IMediator mediator, CreateTransactionRequest request) =>
            {
                var command = CreateTransactionCommand.FromRequest(request);
                var transactionId = await mediator.Send(command);
                return Results.Created($"/api/transactions/{transactionId}", null);
            })
            .WithName("CreateTransaction")
            .WithOpenApi()
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapGroup("/api/transactions")
            .MapGet("/{id}", async (IMediator mediator, Guid id) =>
            {
                var query = new GetTransactionByIdQuery { Id = id };
                var transaction = await mediator.Send(query);
                return transaction is null
                    ? Results.NotFound()
                    : Results.Ok(transaction);
            })
            .WithName("GetTransaction")
            .WithOpenApi()
            .Produces<TransactionDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGroup("/api/transactions")
            .MapGet("/", async (IMediator mediator, string merchantId, DateTime? date = null) =>
            {
                var query = new GetTransactionsByMerchantQuery
                {
                    MerchantId = merchantId,
                    Date = date
                };
                var transactions = await mediator.Send(query);
                return Results.Ok(transactions);
            })
            .WithName("GetTransactions")
            .WithOpenApi()
            .Produces<IEnumerable<TransactionDto>>();

// Ensure database is created
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
            dbContext.Database.Migrate();
        }

        app.Run();
    }
}