using CashFlow.Consolidation.Application.DTOs;
using CashFlow.Consolidation.Application.Queries;
using CashFlow.Consolidation.Domain.Models;
using CashFlow.Consolidation.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Consolidation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyBalancesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDailyBalanceRepository _balanceRepository;
    private readonly ILogger<DailyBalancesController> _logger;

    public DailyBalancesController(
        IMediator mediator,
        IDailyBalanceRepository balanceRepository,
        ILogger<DailyBalancesController> logger)
    {
        _mediator = mediator;
        _balanceRepository = balanceRepository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DailyBalanceResponse>>> GetDailyBalances(
        [FromQuery] string merchantId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var currentDate = DateTime.UtcNow.Date;
            startDate ??= currentDate.AddDays(-30); // Default to last 30 days
            endDate ??= currentDate;

            var balances = new List<DailyBalanceDto>();
            var currentDatePointer = startDate.Value.Date;

            while (currentDatePointer <= endDate.Value.Date)
            {
                var balance = await _balanceRepository.GetByMerchantAndDateAsync(merchantId, currentDatePointer);

                if (balance != null) balances.Add(MapToDto(balance));

                currentDatePointer = currentDatePointer.AddDays(1);
            }

            if (!balances.Any())
                return NotFound($"No balance records found for merchant {merchantId} in the specified date range");

            var responses = balances.Select(DailyBalanceResponse.FromDto).ToList();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving daily balances for merchant {MerchantId} from {StartDate} to {EndDate}",
                merchantId, startDate, endDate);

            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving the balance information");
        }
    }


    [HttpGet("daily")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyBalanceResponse>> GetDailyBalance(
        [FromQuery] string merchantId,
        [FromQuery] DateTime date)
    {
        try
        {
            var query = new GetDailyBalanceQuery
            {
                MerchantId = merchantId,
                Date = date
            };

            var balance = await _mediator.Send(query);

            if (balance == null)
                return NotFound($"No balance record found for merchant {merchantId} on {date:yyyy-MM-dd}");

            return Ok(DailyBalanceResponse.FromDto(balance));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily balance for merchant {MerchantId} on {Date}",
                merchantId, date);

            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving the balance information");
        }
    }

    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetBalanceSummary(
        [FromQuery] string merchantId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var currentDate = DateTime.UtcNow.Date;
            startDate ??= currentDate.AddDays(-30); // Default to last 30 days
            endDate ??= currentDate;

            var balances = new List<DailyBalance>();
            var currentDatePointer = startDate.Value.Date;

            while (currentDatePointer <= endDate.Value.Date)
            {
                var balance = await _balanceRepository.GetByMerchantAndDateAsync(merchantId, currentDatePointer);
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

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating balance summary for merchant {MerchantId} from {StartDate} to {EndDate}",
                merchantId, startDate, endDate);

            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while generating the balance summary");
        }
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
}