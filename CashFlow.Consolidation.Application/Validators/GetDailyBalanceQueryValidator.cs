using CashFlow.Consolidation.Application.Queries;
using FluentValidation;

namespace CashFlow.Consolidation.Application.Validators;

public class GetDailyBalanceQueryValidator : AbstractValidator<GetDailyBalanceQuery>
{
    public GetDailyBalanceQueryValidator()
    {
        RuleFor(query => query.MerchantId)
            .NotEmpty().WithMessage("Merchant ID is required.")
            .MaximumLength(50).WithMessage("Merchant ID cannot exceed 50 characters.");

        RuleFor(query => query.Date)
            .NotEmpty().WithMessage("Date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("Date cannot be in the future.");
    }
}