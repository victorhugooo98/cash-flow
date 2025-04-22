using CashFlow.Consolidation.Application.DTOs;
using CashFlow.Consolidation.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Application.Services;

public class BalanceHistoryService : IBalanceHistoryService
{
    private readonly IDailyBalanceRepository _balanceRepository;
    private readonly ILogger<BalanceHistoryService> _logger;

    public BalanceHistoryService(
        IDailyBalanceRepository balanceRepository,
        ILogger<BalanceHistoryService> logger)
    {
        _balanceRepository = balanceRepository;
        _logger = logger;
    }

    public async Task<BalanceHistoryDto> GetBalanceHistoryAsync(string merchantId, DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation(
            "Retrieving balance history for merchant {MerchantId} from {StartDate} to {EndDate}",
            merchantId, startDate, endDate);

        var history = new BalanceHistoryDto
        {
            MerchantId = merchantId,
            StartDate = startDate,
            EndDate = endDate,
            Entries = new List<BalanceHistoryEntryDto>()
        };

        // Process each day in the date range
        var currentDate = startDate.Date;
        while (currentDate <= endDate.Date)
        {
            var balance = await _balanceRepository.GetByMerchantAndDateAsync(merchantId, currentDate);
            
            if (balance != null)
            {
                history.Entries.Add(new BalanceHistoryEntryDto
                {
                    Date = currentDate,
                    OpeningBalance = balance.OpeningBalance,
                    ClosingBalance = balance.ClosingBalance,
                    TotalCredits = balance.TotalCredits,
                    TotalDebits = balance.TotalDebits,
                    NetChange = balance.TotalCredits - balance.TotalDebits
                });
            }
            
            currentDate = currentDate.AddDays(1);
        }

        // Calculate overall statistics
        if (history.Entries.Any())
        {
            history.TotalCredits = history.Entries.Sum(e => e.TotalCredits);
            history.TotalDebits = history.Entries.Sum(e => e.TotalDebits);
            history.NetChange = history.TotalCredits - history.TotalDebits;
            history.InitialBalance = history.Entries.First().OpeningBalance;
            history.FinalBalance = history.Entries.Last().ClosingBalance;
            history.DaysWithActivity = history.Entries.Count;
            
            // Calculate average daily transaction volumes
            history.AverageDailyCredits = history.Entries.Count > 0 
                ? history.TotalCredits / history.Entries.Count 
                : 0;
            
            history.AverageDailyDebits = history.Entries.Count > 0 
                ? history.TotalDebits / history.Entries.Count 
                : 0;
            
            // Calculate trends
            if (history.Entries.Count >= 2)
            {
                var firstClosingBalance = history.Entries.First().ClosingBalance;
                var lastClosingBalance = history.Entries.Last().ClosingBalance;
                var daysBetween = (endDate - startDate).Days + 1;
                
                history.BalanceTrend = daysBetween > 0 
                    ? (lastClosingBalance - firstClosingBalance) / daysBetween 
                    : 0;
            }
        }

        _logger.LogInformation(
            "Retrieved balance history for merchant {MerchantId} with {EntryCount} days of data",
            merchantId, history.Entries.Count);

        return history;
    }
}

public interface IBalanceHistoryService
{
    Task<BalanceHistoryDto> GetBalanceHistoryAsync(string merchantId, DateTime startDate, DateTime endDate);
}