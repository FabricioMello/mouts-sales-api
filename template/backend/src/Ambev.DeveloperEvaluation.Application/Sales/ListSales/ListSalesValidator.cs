using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesValidator : AbstractValidator<ListSalesCommand>
{
    public ListSalesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);

        RuleFor(x => x.SaleNumber)
            .MaximumLength(50)
            .When(x => x.SaleNumber is not null);

        RuleFor(x => x.CustomerName)
            .MaximumLength(100)
            .When(x => x.CustomerName is not null);

        RuleFor(x => x.BranchName)
            .MaximumLength(100)
            .When(x => x.BranchName is not null);

        RuleFor(x => x.SaleDateTo)
            .GreaterThanOrEqualTo(x => x.SaleDateFrom)
            .When(x => x.SaleDateFrom.HasValue && x.SaleDateTo.HasValue)
            .WithMessage("SaleDateTo must be greater than or equal to SaleDateFrom");
    }
}
