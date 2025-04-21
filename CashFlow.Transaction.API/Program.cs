using CashFlow.Transaction.Application.Commands.CreateTransaction;
using CashFlow.Transaction.Application.Events;
using CashFlow.Transaction.Domain.Repositories;
using CashFlow.Transaction.Infrastructure.Data;
using CashFlow.Transaction.Infrastructure.Messaging;
using CashFlow.Transaction.Infrastructure.Repositories;
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

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTransactionCommand).Assembly));

// Add DbContext
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TransactionDatabase")));

// Add repositories
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Add event publisher
builder.Services.AddScoped<ITransactionEventPublisher, TransactionEventPublisher>();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();