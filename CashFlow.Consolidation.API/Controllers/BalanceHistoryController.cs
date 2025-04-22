using CashFlow.Consolidation.Application.DTOs;
using CashFlow.Consolidation.Application.Queries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Consolidation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BalanceHistoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BalanceHistoryController> _logger;

    public BalanceHistoryController(
        IMediator mediator,
        ILogger<BalanceHistoryController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BalanceHistoryResponse>> GetBalanceHistory(
        [FromQuery] string merchantId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var currentDate = DateTime.UtcNow.Date;
            startDate ??= currentDate.AddDays(-30); // Default to last 30 days
            endDate ??= currentDate;

            var query = new GetBalanceHistoryQuery
            {
                MerchantId = merchantId,
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };

            var balanceHistory = await _mediator.Send(query);

            if (balanceHistory == null || !balanceHistory.Entries.Any())
                return NotFound($"No balance records found for merchant {merchantId} in the specified date range");

            return Ok(BalanceHistoryResponse.FromDto(balanceHistory));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error for balance history request");
            return BadRequest(new
            {
                Errors = ex.Errors.Select(e => new
                {
                    Property = e.PropertyName,
                    Error = e.ErrorMessage
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving balance history for merchant {MerchantId}", merchantId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while retrieving the balance history");
        }
    }

    [HttpGet("trends")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetBalanceTrends(
        [FromQuery] string merchantId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var currentDate = DateTime.UtcNow.Date;
            startDate ??= currentDate.AddDays(-90); // Default to last 90 days
            endDate ??= currentDate;

            var query = new GetBalanceHistoryQuery
            {
                MerchantId = merchantId,
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };

            var balanceHistory = await _mediator.Send(query);

            if (balanceHistory == null || !balanceHistory.Entries.Any())
                return NotFound($"No balance records found for merchant {merchantId} in the specified date range");

            // Calculate weekly and monthly trends
            var weeklyTrends = CalculateWeeklyTrends(balanceHistory);
            var monthlyTrends = CalculateMonthlyTrends(balanceHistory);

            var response = new
            {
                MerchantId = merchantId,
                Period = $"{startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}",
                Overview = new
                {
                    TotalTransactions = balanceHistory.Entries.Count,
                    TotalCredits = balanceHistory.TotalCredits,
                    TotalDebits = balanceHistory.TotalDebits,
                    NetChange = balanceHistory.NetChange,
                    InitialBalance = balanceHistory.InitialBalance,
                    FinalBalance = balanceHistory.FinalBalance,
                    DailyBalanceTrend = balanceHistory.BalanceTrend
                },
                WeeklyTrends = weeklyTrends,
                MonthlyTrends = monthlyTrends
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving balance trends for merchant {MerchantId}", merchantId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while retrieving the balance trends");
        }
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

    private static int GetWeekNumber(DateTime date)
    {
        // ISO 8601 week number calculation
        var day = (int)System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date.AddDays(day == 0 ? -6 : 1 - day),
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
    }

    private static DateTime GetFirstDayOfWeek(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        return date.AddDays(day == 0 ? -6 : 1 - day); // First day is Monday
    }
}