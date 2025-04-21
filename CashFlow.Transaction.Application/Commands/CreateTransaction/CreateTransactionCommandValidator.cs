using CashFlow.Transaction.Application.Commands.CreateTransaction;
using CashFlow.Transaction.Domain.Models;
using FluentValidation;

namespace CashFlow.Transaction.Application.Commands.CreateTransaction
{
    public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
    {
        public CreateTransactionCommandValidator()
        {
            RuleFor(command => command.MerchantId)
                .NotEmpty().WithMessage("Merchant ID is required.")
                .MaximumLength(50).WithMessage("Merchant ID cannot exceed 50 characters.");
                
            RuleFor(command => command.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");
                
            RuleFor(command => command.Type)
                .NotEmpty().WithMessage("Transaction type is required.")
                .Must(type => Enum.TryParse<TransactionType>(type, out _))
                .WithMessage("Transaction type must be either 'Credit' or 'Debit'.");
                
            RuleFor(command => command.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(200).WithMessage("Description cannot exceed 200 characters.");
        }
    }
}