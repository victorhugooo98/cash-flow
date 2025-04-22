using System.Net;
using System.Text.Json;
using CashFlow.Shared.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CashFlow.Shared.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var response = CreateProblemDetails(exception, statusCode);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);

        _logger.LogError(exception, "Request failed with status code {StatusCode}: {Message}",
            statusCode, exception.Message);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            TransactionValidationException => StatusCodes.Status400BadRequest,
            ApiException apiException => apiException.StatusCode,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static object CreateProblemDetails(Exception exception, int statusCode)
    {
        return exception switch
        {
            ValidationException validationException => new
            {
                Title = "Validation Error",
                Status = statusCode,
                Detail = "One or more validation errors occurred",
                Errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            },
            TransactionValidationException validationException => new
            {
                Title = "Transaction Validation Error",
                Status = statusCode,
                Detail = exception.Message,
                Errors = validationException.ValidationErrors
                    .Select(e => new { Description = e })
            },
            _ => new
            {
                Title = GetErrorTitle(exception),
                Status = statusCode,
                Detail = exception.Message,
                TraceId = Guid.NewGuid().ToString()
            }
        };
    }

    private static string GetErrorTitle(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => "Resource Not Found",
            ApiException => "API Error",
            _ => "Server Error"
        };
    }
}