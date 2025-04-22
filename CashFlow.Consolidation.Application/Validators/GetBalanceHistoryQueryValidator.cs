using CashFlow.Consolidation.Application.Queries;
using FluentValidation;

namespace CashFlow.Consolidation.Application.Validators;

public class GetBalanceHistoryQueryValidator : AbstractValidator<GetBalanceHistoryQuery>
{
    public GetBalanceHistoryQueryValidator()
    {
        RuleFor(query => query.MerchantId)
            .NotEmpty().WithMessage("Merchant ID is required.")
            .MaximumLength(50).WithMessage("Merchant ID cannot exceed 50 characters.");

        RuleFor(query => query.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("Start date cannot be in the future.");

        RuleFor(query => query.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("End date cannot be in the future.");

        RuleFor(query => query.EndDate)
            .GreaterThanOrEqualTo(query => query.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");

        RuleFor(query => query)
            .Must(query => (query.EndDate - query.StartDate).TotalDays <= 90)
            .WithMessage("Date range cannot exceed 90 days.")
            .When(query => query.StartDate != default && query.EndDate != default);
    }
}