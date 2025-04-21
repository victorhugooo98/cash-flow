using CashFlow.Transaction.Application.Commands.CreateTransaction;
using CashFlow.Transaction.Application.Events;
using CashFlow.Transaction.Domain.Repositories;
using CashFlow.Transaction.Infrastructure.Data;
using CashFlow.Transaction.Infrastructure.Messaging;
using CashFlow.Transaction.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Microsoft.OpenApi.Models;
using System;
using CashFlow.Shared.Middleware;
using CashFlow.Transaction.API.HealthChecks;
using CashFlow.Transaction.Application.Behaviors;
using MediatR;

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
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateTransactionCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// Add DbContext
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TransactionDatabase")));

// Add repositories
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

builder.Services.AddScoped<DbContextHealthCheck<TransactionDbContext>>();
builder.Services.AddScoped<RabbitMQHealthCheck>(sp => 
    new RabbitMQHealthCheck(
        builder.Configuration["RabbitMQ:Host"] ?? "localhost",
        builder.Configuration["RabbitMQ:Username"] ?? "guest",
        builder.Configuration["RabbitMQ:Password"] ?? "guest"));

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
        cfg.UseMessageRetry(r => 
        {
            r.Interval(3, TimeSpan.FromSeconds(5));
        });
        
        cfg.ConfigureEndpoints(context);
    });
});

// Add event publisher
builder.Services.AddScoped<ITransactionEventPublisher, TransactionEventPublisher>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("db-check", () => HealthCheckResult.Healthy(), tags: new[] { "ready" })
    .AddCheck("rabbitmq-check", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    dbContext.Database.Migrate();
}

app.Run();