using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;
using CashFlow.Transaction.API.HealthChecks;
using CashFlow.Transaction.Infrastructure.Data;

namespace CashFlow.Transaction.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register health check dependencies
        services.AddScoped<DbContextHealthCheck<TransactionDbContext>>();
        services.AddScoped<RabbitMQHealthCheck>(sp => 
            new RabbitMQHealthCheck(
                configuration["RabbitMQ:Host"] ?? "localhost",
                configuration["RabbitMQ:Username"] ?? "guest", 
                configuration["RabbitMQ:Password"] ?? "guest"));
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<DbContextHealthCheck<TransactionDbContext>>(
                "database",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"])
            .AddCheck<RabbitMQHealthCheck>(
                "rabbitmq",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"]);

        return services;
    }

    public static IApplicationBuilder UseCustomHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteResponse
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteResponse
        });

        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteResponse
        });

        return app;
    }

    private static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonWriterOptions { Indented = true };
        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", healthReport.Status.ToString());
            jsonWriter.WriteStartObject("checks");

            foreach (var healthReportEntry in healthReport.Entries)
            {
                jsonWriter.WriteStartObject(healthReportEntry.Key);
                jsonWriter.WriteString("status", 
                    healthReportEntry.Value.Status.ToString());
                jsonWriter.WriteString("description", 
                    healthReportEntry.Value.Description);
                
                jsonWriter.WriteStartObject("data");
                foreach (var item in healthReportEntry.Value.Data)
                {
                    jsonWriter.WritePropertyName(item.Key);
                    JsonSerializer.Serialize(jsonWriter, item.Value, 
                        item.Value?.GetType() ?? typeof(object));
                }
                jsonWriter.WriteEndObject();
                
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        return context.Response.WriteAsync(
            Encoding.UTF8.GetString(memoryStream.ToArray()));
    }
}