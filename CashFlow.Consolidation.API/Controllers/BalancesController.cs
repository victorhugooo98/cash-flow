using CashFlow.Consolidation.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Consolidation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BalancesController : ControllerBase
{
    private readonly IDailyBalanceRepository _balanceRepository;
    private readonly ILogger<BalancesController> _logger;

    public BalancesController(
        IDailyBalanceRepository balanceRepository,
        ILogger<BalancesController> logger)
    {
        _balanceRepository = balanceRepository;
        _logger = logger;
    }

    [HttpGet("daily")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDailyBalance(
        [FromQuery] string merchantId,
        [FromQuery] DateTime date)
    {
        try
        {
            var balance = await _balanceRepository.GetByMerchantAndDateAsync(merchantId, date);

            if (balance == null)
                return NotFound($"No balance record found for merchant {merchantId} on {date:yyyy-MM-dd}");

            var balanceResponse = new
            {
                balance.Id,
                balance.MerchantId,
                Date = balance.Date.ToString("yyyy-MM-dd"),
                balance.OpeningBalance,
                balance.TotalCredits,
                balance.TotalDebits,
                balance.ClosingBalance
            };

            return Ok(balanceResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily balance for merchant {MerchantId} on {Date}",
                merchantId, date);

            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving the balance information");
        }
    }
}