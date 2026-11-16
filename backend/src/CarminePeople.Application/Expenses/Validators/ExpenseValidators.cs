using CarminePeople.Application.Expenses.DTOs;
using FluentValidation;

namespace CarminePeople.Application.Expenses.Validators;

public class SubmitExpenseRequestValidator : AbstractValidator<SubmitExpenseRequest>
{
    public SubmitExpenseRequestValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ExpenseDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
        RuleFor(x => x.ReceiptUrl).MaximumLength(500).When(x => x.ReceiptUrl != null);
    }
}

public class ExpenseDecisionRequestValidator : AbstractValidator<ExpenseDecisionRequest>
{
    public ExpenseDecisionRequestValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment != null);
    }
}
