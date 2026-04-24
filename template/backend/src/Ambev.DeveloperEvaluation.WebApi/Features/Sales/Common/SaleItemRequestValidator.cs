using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

public class SaleItemRequestValidator : AbstractValidator<SaleItemRequest>
{
    public SaleItemRequestValidator()
    {
        RuleFor(item => item.ProductId).NotEmpty();
        RuleFor(item => item.ProductName).NotEmpty().MaximumLength(100);
        RuleFor(item => item.Quantity).GreaterThan(0).LessThanOrEqualTo(20);
        RuleFor(item => item.UnitPrice).GreaterThan(0);
    }
}
