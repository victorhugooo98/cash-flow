using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CashFlow.Consolidation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        var response = new
        {
            Status = report.Status.ToString(),
            Components = report.Entries.Select(e => new
            {
                Component = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration.TotalMilliseconds
            }),
            TotalDuration = report.TotalDuration.TotalMilliseconds
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        var report = await _healthCheckService.CheckHealthAsync(registration => registration.Tags.Contains("ready"));

        return report.Status == HealthStatus.Healthy
            ? Ok("Service is ready")
            : StatusCode(StatusCodes.Status503ServiceUnavailable, "Service is not ready");
    }

    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        return Ok("Service is running");
    }
}