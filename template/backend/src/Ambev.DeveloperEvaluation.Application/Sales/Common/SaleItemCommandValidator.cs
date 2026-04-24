using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

public class SaleItemCommandValidator : AbstractValidator<SaleItemCommand>
{
    public SaleItemCommandValidator()
    {
        RuleFor(item => item.ProductId).NotEmpty();
        RuleFor(item => item.ProductName).NotEmpty().MaximumLength(100);
        RuleFor(item => item.Quantity).GreaterThan(0).LessThanOrEqualTo(20);
        RuleFor(item => item.UnitPrice).GreaterThan(0);
    }
}
