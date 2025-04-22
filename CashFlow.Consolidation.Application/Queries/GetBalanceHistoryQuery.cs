using CashFlow.Consolidation.Application.DTOs;
using MediatR;

namespace CashFlow.Consolidation.Application.Queries;

public class GetBalanceHistoryQuery : IRequest<BalanceHistoryDto>
{
    public string MerchantId { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}