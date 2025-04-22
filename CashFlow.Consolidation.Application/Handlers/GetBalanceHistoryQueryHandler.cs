using CashFlow.Consolidation.Application.DTOs;
using CashFlow.Consolidation.Application.Queries;
using CashFlow.Consolidation.Application.Services;
using MediatR;

namespace CashFlow.Consolidation.Application.Handlers;

public class GetBalanceHistoryQueryHandler : IRequestHandler<GetBalanceHistoryQuery, BalanceHistoryDto>
{
    private readonly IBalanceHistoryService _balanceHistoryService;

    public GetBalanceHistoryQueryHandler(IBalanceHistoryService balanceHistoryService)
    {
        _balanceHistoryService = balanceHistoryService;
    }

    public async Task<BalanceHistoryDto> Handle(GetBalanceHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _balanceHistoryService.GetBalanceHistoryAsync(
            request.MerchantId,
            request.StartDate,
            request.EndDate);
    }
}