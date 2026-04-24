using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesValidator : AbstractValidator<ListSalesCommand>
{
    public ListSalesValidator()
    {
        RuleFor(sales => sales.Page).GreaterThan(0);
        RuleFor(sales => sales.Size).InclusiveBetween(1, 100);
    }
}
