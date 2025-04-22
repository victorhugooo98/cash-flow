using CashFlow.Consolidation.Application.DTOs;
using CashFlow.Consolidation.Application.Queries;
using CashFlow.Consolidation.Domain.Repositories;
using MediatR;

namespace CashFlow.Consolidation.Application.Handlers;

public class GetDailyBalanceQueryHandler : IRequestHandler<GetDailyBalanceQuery, DailyBalanceDto?>
{
    private readonly IDailyBalanceRepository _balanceRepository;

    public GetDailyBalanceQueryHandler(IDailyBalanceRepository balanceRepository)
    {
        _balanceRepository = balanceRepository;
    }

    public async Task<DailyBalanceDto?> Handle(GetDailyBalanceQuery request, CancellationToken cancellationToken)
    {
        var balance = await _balanceRepository.GetByMerchantAndDateAsync(request.MerchantId, request.Date);

        if (balance == null)
            return null;

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