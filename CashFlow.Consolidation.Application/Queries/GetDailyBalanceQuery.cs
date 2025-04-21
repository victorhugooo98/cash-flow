using MediatR;
using CashFlow.Consolidation.Application.DTOs;

namespace CashFlow.Consolidation.Application.Queries
{
    public class GetDailyBalanceQuery : IRequest<DailyBalanceDto>
    {
        public string MerchantId { get; set; }
        public DateTime Date { get; set; }
    }
}